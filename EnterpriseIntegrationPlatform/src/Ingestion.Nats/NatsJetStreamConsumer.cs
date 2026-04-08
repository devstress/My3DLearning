using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace EnterpriseIntegrationPlatform.Ingestion.Nats;

/// <summary>
/// Consumes <see cref="IntegrationEnvelope{T}"/> messages from NATS JetStream.
/// Uses queue groups so that within a consumer group each message is delivered
/// to exactly one consumer — recipient A does not block recipient B.
/// </summary>
public sealed class NatsJetStreamConsumer : IMessageBrokerConsumer
{
    internal static readonly ActivitySource ActivitySource = new("EIP.Ingestion.Nats.Consumer");

    private readonly INatsConnection _connection;
    private readonly INatsJSContext _js;
    private readonly ILogger<NatsJetStreamConsumer> _logger;
    private readonly CancellationTokenSource _disposeCts = new();
    private bool _disposed;

    /// <summary>Initialises a new <see cref="NatsJetStreamConsumer"/>.</summary>
    public NatsJetStreamConsumer(INatsConnection connection, ILogger<NatsJetStreamConsumer> logger)
    {
        _connection = connection;
        _js = new NatsJSContext((NatsConnection)connection);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);
        ArgumentNullException.ThrowIfNull(handler);

        var streamName = topic.Replace(".", "-");

        // Link caller's token with the dispose token so DisposeAsync cancels active subscriptions.
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);

        await EnsureStreamAsync(streamName, topic, linked.Token);

        var consumer = await _js.CreateOrUpdateConsumerAsync(
            streamName,
            new ConsumerConfig(consumerGroup)
            {
                FilterSubject = topic,
                DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
            },
            linked.Token);

        _logger.LogInformation(
            "Subscribed to NATS JetStream subject {Subject} with consumer group {Group}",
            topic, consumerGroup);

        await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: linked.Token))
        {
            using var activity = ActivitySource.StartActivity("NatsJetStream.Consume");
            activity?.SetTag("messaging.system", "nats");
            activity?.SetTag("messaging.destination", topic);

            try
            {
                if (msg.Data is null)
                {
                    _logger.LogWarning("Received null data on subject {Subject}", topic);
                    await msg.AckAsync(cancellationToken: linked.Token);
                    continue;
                }

                var envelope = EnvelopeSerializer.Deserialize<T>(msg.Data);
                if (envelope is null)
                {
                    _logger.LogWarning(
                        "Failed to deserialise message on subject {Subject}", topic);
                    await msg.AckAsync(cancellationToken: linked.Token);
                    continue;
                }

                activity?.SetTag("messaging.message_id", envelope.MessageId.ToString());

                await handler(envelope);
                await msg.AckAsync(cancellationToken: linked.Token);

                _logger.LogDebug(
                    "Processed message {MessageId} from NATS subject {Subject}",
                    envelope.MessageId, topic);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "Error processing message from NATS subject {Subject}", topic);
                await msg.NakAsync(cancellationToken: linked.Token);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _disposeCts.CancelAsync();
        _disposeCts.Dispose();
    }

    private async Task EnsureStreamAsync(string streamName, string topic, CancellationToken ct)
    {
        try
        {
            await _js.GetStreamAsync(streamName, cancellationToken: ct);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            await _js.CreateStreamAsync(
                new StreamConfig(streamName, [topic]),
                ct);
        }
    }
}
