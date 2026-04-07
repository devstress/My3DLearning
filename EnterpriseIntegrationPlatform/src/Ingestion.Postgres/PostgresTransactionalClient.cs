using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Postgres;

/// <summary>
/// PostgreSQL transactional client — provides true ACID atomicity for message
/// publish operations via <see cref="Npgsql.NpgsqlTransaction"/>.
/// All messages published within the transaction scope are committed atomically
/// or rolled back on failure. No compensation needed.
/// </summary>
public sealed class PostgresTransactionalClient : ITransactionalClient
{
    private readonly PostgresConnectionFactory _factory;
    private readonly ILogger<PostgresTransactionalClient> _logger;

    public PostgresTransactionalClient(
        PostgresConnectionFactory factory,
        ILogger<PostgresTransactionalClient> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// PostgreSQL supports native transactions — no compensation needed.
    /// </summary>
    public bool SupportsNativeTransactions => true;

    /// <inheritdoc />
    public async Task<TransactionResult> ExecuteAsync(
        Func<ITransactionScope, CancellationToken, Task> operations,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _factory.OpenConnectionAsync(cancellationToken);
        await using var txn = await conn.BeginTransactionAsync(cancellationToken);

        var scope = new PostgresTransactionScope(conn, txn);

        try
        {
            await operations(scope, cancellationToken);
            await txn.CommitAsync(cancellationToken);

            _logger.LogDebug(
                "Postgres transaction committed with {Count} message(s)",
                scope.PublishedCount);

            return TransactionResult.Success(scope.PublishedCount, TimeSpan.Zero);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Postgres transaction rolled back");
            await txn.RollbackAsync(cancellationToken);
            return TransactionResult.Failure(ex.Message, ex);
        }
    }
}

/// <summary>
/// Transaction scope backed by a real PostgreSQL transaction.
/// Messages published within this scope are part of the same DB transaction.
/// </summary>
internal sealed class PostgresTransactionScope : ITransactionScope
{
    private readonly Npgsql.NpgsqlConnection _conn;
    private readonly Npgsql.NpgsqlTransaction _txn;
    private int _publishedCount;

    private const string InsertSql = """
        INSERT INTO eip_messages (message_id, topic, payload)
        VALUES ($1, $2, $3::jsonb)
        """;

    public PostgresTransactionScope(
        Npgsql.NpgsqlConnection conn,
        Npgsql.NpgsqlTransaction txn)
    {
        _conn = conn;
        _txn = txn;
    }

    public int PublishedCount => _publishedCount;

    public async Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var json = JsonSerializer.Serialize(envelope, EnvelopeSerializerOptions.Default);

        await using var cmd = _conn.CreateCommand();
        cmd.Transaction = _txn;
        cmd.CommandText = InsertSql;
        cmd.Parameters.AddWithValue(envelope.MessageId);
        cmd.Parameters.AddWithValue(topic);
        cmd.Parameters.AddWithValue(json);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        Interlocked.Increment(ref _publishedCount);
    }
}
