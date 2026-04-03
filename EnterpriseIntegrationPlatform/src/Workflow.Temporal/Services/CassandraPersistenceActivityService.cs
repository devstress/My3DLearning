using Microsoft.Extensions.Logging;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Storage.Cassandra;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal.Services;

/// <summary>
/// Persistence activity service backed by Cassandra.
/// Used by <see cref="Temporal.Activities.PipelineActivities"/> to execute durable
/// persistence operations as Temporal activities.
/// </summary>
public sealed class CassandraPersistenceActivityService : IPersistenceActivityService
{
    private readonly IMessageRepository _repository;
    private readonly ILogger<CassandraPersistenceActivityService> _logger;

    public CassandraPersistenceActivityService(
        IMessageRepository repository,
        ILogger<CassandraPersistenceActivityService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveMessageAsync(
        IntegrationPipelineInput input,
        CancellationToken cancellationToken = default)
    {
        var record = new MessageRecord
        {
            MessageId = input.MessageId,
            CorrelationId = input.CorrelationId,
            CausationId = input.CausationId,
            RecordedAt = input.Timestamp,
            Source = input.Source,
            MessageType = input.MessageType,
            SchemaVersion = input.SchemaVersion,
            Priority = (MessagePriority)input.Priority,
            PayloadJson = input.PayloadJson,
            MetadataJson = input.MetadataJson,
            DeliveryStatus = DeliveryStatus.Pending,
        };

        await _repository.SaveMessageAsync(record, cancellationToken);

        _logger.LogDebug(
            "Persisted message {MessageId} as Pending",
            input.MessageId);
    }

    /// <inheritdoc />
    public async Task UpdateDeliveryStatusAsync(
        Guid messageId,
        Guid correlationId,
        DateTimeOffset recordedAt,
        string status,
        CancellationToken cancellationToken = default)
    {
        var deliveryStatus = Enum.Parse<DeliveryStatus>(status);

        await _repository.UpdateDeliveryStatusAsync(
            messageId, correlationId, recordedAt, deliveryStatus, cancellationToken);

        _logger.LogDebug(
            "Updated message {MessageId} status to {Status}",
            messageId, status);
    }

    /// <inheritdoc />
    public async Task SaveFaultAsync(
        Guid messageId,
        Guid correlationId,
        string messageType,
        string faultedBy,
        string reason,
        int retryCount,
        CancellationToken cancellationToken = default)
    {
        var fault = new FaultEnvelope
        {
            FaultId = Guid.NewGuid(),
            OriginalMessageId = messageId,
            CorrelationId = correlationId,
            OriginalMessageType = messageType,
            FaultedBy = faultedBy,
            FaultReason = reason,
            FaultedAt = DateTimeOffset.UtcNow,
            RetryCount = retryCount,
        };

        await _repository.SaveFaultAsync(fault, cancellationToken);

        _logger.LogDebug(
            "Saved fault for message {MessageId}: {Reason}",
            messageId, reason);
    }
}
