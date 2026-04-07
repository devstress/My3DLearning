using System.Collections.Concurrent;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace EnterpriseIntegrationPlatform.Ingestion.Postgres;

/// <summary>
/// PostgreSQL-backed message broker consumer. Supports:
/// <list type="bullet">
///   <item><description>Event-driven delivery via <c>LISTEN</c>/<c>pg_notify</c></description></item>
///   <item><description>Polling fallback via <c>SELECT … FOR UPDATE SKIP LOCKED</c></description></item>
///   <item><description>Competing consumers via row-level locks</description></item>
///   <item><description>Selective consumption via predicate filtering</description></item>
/// </list>
/// </summary>
public sealed class PostgresBrokerConsumer : IMessageBrokerConsumer,
    IEventDrivenConsumer, IPollingConsumer, ISelectiveConsumer, IAsyncDisposable
{
    private readonly PostgresConnectionFactory _factory;
    private readonly PostgresBrokerOptions _options;
    private readonly ILogger<PostgresBrokerConsumer> _logger;
    private readonly List<CancellationTokenSource> _subscriptions = new();

    // SQL: fetch and lock pending messages for a consumer group.
    // SKIP LOCKED ensures competing consumers don't block each other.
    private const string FetchAndLockSql = """
        WITH pending AS (
            SELECT s.id, s.message_id
            FROM eip_subscriptions s
            WHERE s.topic = $1
              AND s.consumer_group = $2
              AND s.delivered_at IS NULL
              AND (s.locked_until IS NULL OR s.locked_until < now())
            ORDER BY s.id
            LIMIT $3
            FOR UPDATE OF s SKIP LOCKED
        )
        UPDATE eip_subscriptions sub
        SET locked_until = now() + make_interval(secs => $4),
            locked_by = $5
        FROM pending
        WHERE sub.id = pending.id
        RETURNING sub.message_id, (SELECT m.payload FROM eip_messages m WHERE m.id = sub.message_id) AS payload
        """;

    private const string AckSql = """
        UPDATE eip_subscriptions
        SET delivered_at = now(), locked_until = NULL, locked_by = NULL
        WHERE message_id = $1 AND consumer_group = $2
        """;

    private const string EnsureSubscriberSql = """
        INSERT INTO eip_durable_subscribers (topic, consumer_group)
        VALUES ($1, $2)
        ON CONFLICT (topic, consumer_group) DO NOTHING
        """;

    // Backfill subscription rows for existing messages when a new consumer group registers
    private const string BackfillSql = """
        INSERT INTO eip_subscriptions (message_id, topic, consumer_group)
        SELECT m.id, m.topic, $2
        FROM eip_messages m
        WHERE m.topic = $1
        ON CONFLICT (consumer_group, message_id) DO NOTHING
        """;

    public PostgresBrokerConsumer(
        PostgresConnectionFactory factory,
        IOptions<PostgresBrokerOptions> options,
        ILogger<PostgresBrokerConsumer> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        await EnsureDurableSubscriberAsync(topic, consumerGroup, cancellationToken);
        StartConsumerLoop(topic, consumerGroup, handler, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task StartAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        await SubscribeAsync(topic, consumerGroup, handler, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IntegrationEnvelope<T>>> PollAsync<T>(
        string topic,
        string consumerGroup,
        int maxMessages = 10,
        CancellationToken cancellationToken = default)
    {
        await EnsureDurableSubscriberAsync(topic, consumerGroup, cancellationToken);
        var results = new List<IntegrationEnvelope<T>>();

        await using var conn = await _factory.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = FetchAndLockSql;
        cmd.Parameters.AddWithValue(topic);
        cmd.Parameters.AddWithValue(consumerGroup);
        cmd.Parameters.AddWithValue(maxMessages);
        cmd.Parameters.AddWithValue((double)_options.LockTimeoutSeconds);
        cmd.Parameters.AddWithValue(Environment.MachineName);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var messageId = reader.GetInt64(0);
            var payload = reader.GetString(1);
            var envelope = JsonSerializer.Deserialize<IntegrationEnvelope<T>>(
                payload, EnvelopeSerializerOptions.Default);

            if (envelope is not null)
            {
                results.Add(envelope);
                // Auto-ACK on poll
                await AckMessageAsync(messageId, consumerGroup, cancellationToken);
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, bool> predicate,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        await EnsureDurableSubscriberAsync(topic, consumerGroup, cancellationToken);
        StartConsumerLoop(topic, consumerGroup, handler, predicate, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var cts in _subscriptions)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }
        _subscriptions.Clear();
    }

    // ── Private helpers ─────────────────────────────────────────────────

    private async Task EnsureDurableSubscriberAsync(
        string topic, string consumerGroup, CancellationToken ct)
    {
        await using var conn = await _factory.OpenConnectionAsync(ct);

        await using var cmd1 = conn.CreateCommand();
        cmd1.CommandText = EnsureSubscriberSql;
        cmd1.Parameters.AddWithValue(topic);
        cmd1.Parameters.AddWithValue(consumerGroup);
        await cmd1.ExecuteNonQueryAsync(ct);

        // Backfill any messages already in the topic
        await using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = BackfillSql;
        cmd2.Parameters.AddWithValue(topic);
        cmd2.Parameters.AddWithValue(consumerGroup);
        await cmd2.ExecuteNonQueryAsync(ct);
    }

    private void StartConsumerLoop<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        Func<IntegrationEnvelope<T>, bool>? predicate,
        CancellationToken externalToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        _subscriptions.Add(cts);

        _ = Task.Run(async () =>
        {
            // Open a dedicated LISTEN connection
            await using var listenConn = await _factory.OpenConnectionAsync(cts.Token);
            var channelName = "eip_" + topic;
            await using var listenCmd = listenConn.CreateCommand();
            listenCmd.CommandText = $"LISTEN \"{channelName}\"";
            await listenCmd.ExecuteNonQueryAsync(cts.Token);

            listenConn.Notification += (_, _) => { /* wake up polling loop */ };

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    // Fetch and process pending messages
                    await ProcessPendingMessagesAsync(
                        topic, consumerGroup, handler, predicate, cts.Token);

                    // Wait for notification or poll interval
                    await listenConn.WaitAsync(
                        TimeSpan.FromMilliseconds(_options.PollIntervalMs),
                        cts.Token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error in consumer loop for topic '{Topic}', group '{Group}'",
                        topic, consumerGroup);
                    await Task.Delay(Math.Min(_options.PollIntervalMs, 5000), cts.Token);
                }
            }
        }, cts.Token);
    }

    private async Task ProcessPendingMessagesAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        Func<IntegrationEnvelope<T>, bool>? predicate,
        CancellationToken ct)
    {
        await using var conn = await _factory.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = FetchAndLockSql;
        cmd.Parameters.AddWithValue(topic);
        cmd.Parameters.AddWithValue(consumerGroup);
        cmd.Parameters.AddWithValue(_options.PollBatchSize);
        cmd.Parameters.AddWithValue((double)_options.LockTimeoutSeconds);
        cmd.Parameters.AddWithValue(Environment.MachineName);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var messageId = reader.GetInt64(0);
            var payload = reader.GetString(1);
            var envelope = JsonSerializer.Deserialize<IntegrationEnvelope<T>>(
                payload, EnvelopeSerializerOptions.Default);

            if (envelope is null) continue;

            if (predicate is not null && !predicate(envelope))
            {
                // Release lock without ACK — message will be redelivered
                continue;
            }

            await handler(envelope);
            await AckMessageAsync(messageId, consumerGroup, ct);
        }
    }

    private async Task AckMessageAsync(
        long messageId, string consumerGroup, CancellationToken ct)
    {
        await using var conn = await _factory.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = AckSql;
        cmd.Parameters.AddWithValue(messageId);
        cmd.Parameters.AddWithValue(consumerGroup);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
