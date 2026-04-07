using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Postgres;

/// <summary>
/// PostgreSQL-backed message broker producer. Each <see cref="PublishAsync{T}"/>
/// call INSERTs a row into <c>eip_messages</c>. A database trigger fires
/// <c>pg_notify('eip_' || topic, id)</c> for low-latency consumer wake-up.
/// </summary>
public sealed class PostgresBrokerProducer : IMessageBrokerProducer
{
    private readonly PostgresConnectionFactory _factory;
    private readonly ILogger<PostgresBrokerProducer> _logger;

    private const string InsertSql = """
        INSERT INTO eip_messages (message_id, topic, payload)
        VALUES ($1, $2, $3::jsonb)
        RETURNING id
        """;

    public PostgresBrokerProducer(
        PostgresConnectionFactory factory,
        ILogger<PostgresBrokerProducer> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var json = JsonSerializer.Serialize(envelope, EnvelopeSerializerOptions.Default);

        await using var conn = await _factory.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = InsertSql;
        cmd.Parameters.AddWithValue(envelope.MessageId);
        cmd.Parameters.AddWithValue(topic);
        cmd.Parameters.AddWithValue(json);

        var id = await cmd.ExecuteScalarAsync(cancellationToken);
        _logger.LogDebug(
            "Published message {MessageId} to topic '{Topic}' (row {RowId})",
            envelope.MessageId, topic, id);
    }
}

/// <summary>
/// Shared JSON serializer options for envelope serialization.
/// </summary>
internal static class EnvelopeSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };
}
