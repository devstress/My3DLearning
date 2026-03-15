using System.Diagnostics;
using Cassandra;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Storage.Cassandra;

/// <summary>
/// Cassandra-backed implementation of <see cref="IMessageRepository"/>.
/// Writes are performed as unlogged batches across denormalised tables
/// to maintain consistency. All operations emit OpenTelemetry spans via
/// the shared <see cref="CassandraDiagnostics.ActivitySource"/>.
/// </summary>
public sealed class CassandraMessageRepository : IMessageRepository
{
    private readonly ICassandraSessionFactory _sessionFactory;
    private readonly ILogger<CassandraMessageRepository> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="CassandraMessageRepository"/>.
    /// </summary>
    /// <param name="sessionFactory">Factory that provides the Cassandra session.</param>
    /// <param name="logger">Logger.</param>
    public CassandraMessageRepository(
        ICassandraSessionFactory sessionFactory,
        ILogger<CassandraMessageRepository> logger)
    {
        _sessionFactory = sessionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveMessageAsync(MessageRecord record, CancellationToken cancellationToken = default)
    {
        using var activity = CassandraDiagnostics.ActivitySource.StartActivity("Cassandra.SaveMessage");
        activity?.SetTag("db.cassandra.message_id", record.MessageId.ToString());
        activity?.SetTag("db.cassandra.correlation_id", record.CorrelationId.ToString());

        var session = await _sessionFactory.GetSessionAsync(cancellationToken);

        var recordedAt = record.RecordedAt.UtcDateTime;

        var insertByCorrelation = new SimpleStatement(
            @"INSERT INTO messages_by_correlation_id
              (correlation_id, message_id, causation_id, recorded_at, source, message_type,
               schema_version, priority, payload_json, metadata_json, delivery_status)
              VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            record.CorrelationId, record.MessageId, record.CausationId,
            recordedAt, record.Source, record.MessageType,
            record.SchemaVersion, (int)record.Priority,
            record.PayloadJson, record.MetadataJson, (int)record.DeliveryStatus);

        var insertById = new SimpleStatement(
            @"INSERT INTO messages_by_id
              (message_id, correlation_id, causation_id, recorded_at, source, message_type,
               schema_version, priority, payload_json, metadata_json, delivery_status)
              VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            record.MessageId, record.CorrelationId, record.CausationId,
            recordedAt, record.Source, record.MessageType,
            record.SchemaVersion, (int)record.Priority,
            record.PayloadJson, record.MetadataJson, (int)record.DeliveryStatus);

        var batch = new BatchStatement()
            .SetBatchType(BatchType.Unlogged)
            .Add(insertByCorrelation)
            .Add(insertById);

        await session.ExecuteAsync(batch);

        _logger.LogDebug(
            "Saved message {MessageId} (correlation={CorrelationId}) to Cassandra",
            record.MessageId, record.CorrelationId);
    }

    /// <inheritdoc />
    public async Task SaveFaultAsync(FaultEnvelope fault, CancellationToken cancellationToken = default)
    {
        using var activity = CassandraDiagnostics.ActivitySource.StartActivity("Cassandra.SaveFault");
        activity?.SetTag("db.cassandra.fault_id", fault.FaultId.ToString());
        activity?.SetTag("db.cassandra.correlation_id", fault.CorrelationId.ToString());

        var session = await _sessionFactory.GetSessionAsync(cancellationToken);

        var faultedAt = fault.FaultedAt.UtcDateTime;

        var insert = new SimpleStatement(
            @"INSERT INTO faults_by_correlation_id
              (correlation_id, fault_id, original_message_id, original_message_type,
               faulted_by, fault_reason, faulted_at, retry_count, error_details, original_payload_json)
              VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            fault.CorrelationId, fault.FaultId, fault.OriginalMessageId,
            fault.OriginalMessageType, fault.FaultedBy, fault.FaultReason,
            faultedAt, fault.RetryCount, fault.ErrorDetails, fault.OriginalPayloadJson);

        await session.ExecuteAsync(insert);

        _logger.LogDebug(
            "Saved fault {FaultId} for message {OriginalMessageId} (correlation={CorrelationId}) to Cassandra",
            fault.FaultId, fault.OriginalMessageId, fault.CorrelationId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MessageRecord>> GetByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        using var activity = CassandraDiagnostics.ActivitySource.StartActivity("Cassandra.GetByCorrelationId");
        activity?.SetTag("db.cassandra.correlation_id", correlationId.ToString());

        var session = await _sessionFactory.GetSessionAsync(cancellationToken);

        var statement = new SimpleStatement(
            "SELECT * FROM messages_by_correlation_id WHERE correlation_id = ?",
            correlationId);

        var resultSet = await session.ExecuteAsync(statement);

        var records = resultSet.Select(MapToMessageRecord).ToList();

        activity?.SetTag("db.cassandra.result_count", records.Count);
        return records;
    }

    /// <inheritdoc />
    public async Task<MessageRecord?> GetByMessageIdAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        using var activity = CassandraDiagnostics.ActivitySource.StartActivity("Cassandra.GetByMessageId");
        activity?.SetTag("db.cassandra.message_id", messageId.ToString());

        var session = await _sessionFactory.GetSessionAsync(cancellationToken);

        var statement = new SimpleStatement(
            "SELECT * FROM messages_by_id WHERE message_id = ?",
            messageId);

        var resultSet = await session.ExecuteAsync(statement);
        var row = resultSet.FirstOrDefault();

        return row is null ? null : MapToMessageRecord(row);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FaultEnvelope>> GetFaultsByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        using var activity = CassandraDiagnostics.ActivitySource.StartActivity("Cassandra.GetFaultsByCorrelationId");
        activity?.SetTag("db.cassandra.correlation_id", correlationId.ToString());

        var session = await _sessionFactory.GetSessionAsync(cancellationToken);

        var statement = new SimpleStatement(
            "SELECT * FROM faults_by_correlation_id WHERE correlation_id = ?",
            correlationId);

        var resultSet = await session.ExecuteAsync(statement);

        var faults = resultSet.Select(MapToFaultEnvelope).ToList();

        activity?.SetTag("db.cassandra.result_count", faults.Count);
        return faults;
    }

    /// <inheritdoc />
    public async Task UpdateDeliveryStatusAsync(
        Guid messageId,
        Guid correlationId,
        DateTimeOffset recordedAt,
        DeliveryStatus status,
        CancellationToken cancellationToken = default)
    {
        using var activity = CassandraDiagnostics.ActivitySource.StartActivity("Cassandra.UpdateDeliveryStatus");
        activity?.SetTag("db.cassandra.message_id", messageId.ToString());
        activity?.SetTag("db.cassandra.delivery_status", status.ToString());

        var session = await _sessionFactory.GetSessionAsync(cancellationToken);

        var recordedAtUtc = recordedAt.UtcDateTime;

        var updateByCorrelation = new SimpleStatement(
            @"UPDATE messages_by_correlation_id
              SET delivery_status = ?
              WHERE correlation_id = ? AND recorded_at = ? AND message_id = ?",
            (int)status, correlationId, recordedAtUtc, messageId);

        var updateById = new SimpleStatement(
            "UPDATE messages_by_id SET delivery_status = ? WHERE message_id = ?",
            (int)status, messageId);

        var batch = new BatchStatement()
            .SetBatchType(BatchType.Unlogged)
            .Add(updateByCorrelation)
            .Add(updateById);

        await session.ExecuteAsync(batch);

        _logger.LogDebug(
            "Updated delivery status of message {MessageId} to {Status}",
            messageId, status);
    }

    private static MessageRecord MapToMessageRecord(Row row)
    {
        return new MessageRecord
        {
            MessageId = row.GetValue<Guid>("message_id"),
            CorrelationId = row.GetValue<Guid>("correlation_id"),
            CausationId = row.IsNull("causation_id") ? null : row.GetValue<Guid>("causation_id"),
            RecordedAt = new DateTimeOffset(row.GetValue<DateTime>("recorded_at"), TimeSpan.Zero),
            Source = row.GetValue<string>("source"),
            MessageType = row.GetValue<string>("message_type"),
            SchemaVersion = row.GetValue<string>("schema_version"),
            Priority = (MessagePriority)row.GetValue<int>("priority"),
            PayloadJson = row.GetValue<string>("payload_json"),
            MetadataJson = row.IsNull("metadata_json") ? null : row.GetValue<string>("metadata_json"),
            DeliveryStatus = (DeliveryStatus)row.GetValue<int>("delivery_status"),
        };
    }

    private static FaultEnvelope MapToFaultEnvelope(Row row)
    {
        return new FaultEnvelope
        {
            FaultId = row.GetValue<Guid>("fault_id"),
            OriginalMessageId = row.GetValue<Guid>("original_message_id"),
            CorrelationId = row.GetValue<Guid>("correlation_id"),
            OriginalMessageType = row.GetValue<string>("original_message_type"),
            FaultedBy = row.GetValue<string>("faulted_by"),
            FaultReason = row.GetValue<string>("fault_reason"),
            FaultedAt = new DateTimeOffset(row.GetValue<DateTime>("faulted_at"), TimeSpan.Zero),
            RetryCount = row.GetValue<int>("retry_count"),
            ErrorDetails = row.IsNull("error_details") ? null : row.GetValue<string>("error_details"),
            OriginalPayloadJson = row.IsNull("original_payload_json") ? null : row.GetValue<string>("original_payload_json"),
        };
    }
}
