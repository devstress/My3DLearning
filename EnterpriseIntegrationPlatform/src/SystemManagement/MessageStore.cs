using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Production implementation of the Message Store Enterprise Integration Pattern.
/// Wraps <see cref="IMessageRepository"/> to provide system management query capabilities.
/// </summary>
public sealed class MessageStore : IMessageStore
{
    private readonly IMessageRepository _repository;
    private readonly ILogger<MessageStore> _logger;

    /// <summary>Initialises a new instance of <see cref="MessageStore"/>.</summary>
    public MessageStore(
        IMessageRepository repository,
        ILogger<MessageStore> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MessageStoreEntry>> GetTrailAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var records = await _repository.GetByCorrelationIdAsync(correlationId, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "Message Store: retrieved {Count} records for CorrelationId {CorrelationId}",
            records.Count, correlationId);

        return records.Select(r => new MessageStoreEntry(
            r.MessageId,
            r.CorrelationId,
            r.MessageType,
            r.Source,
            r.DeliveryStatus,
            r.RecordedAt)).ToList();
    }

    /// <inheritdoc />
    public async Task<MessageStoreEntry?> GetByIdAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var record = await _repository.GetByMessageIdAsync(messageId, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            _logger.LogDebug("Message Store: no record found for MessageId {MessageId}", messageId);
            return null;
        }

        return new MessageStoreEntry(
            record.MessageId,
            record.CorrelationId,
            record.MessageType,
            record.Source,
            record.DeliveryStatus,
            record.RecordedAt);
    }

    /// <inheritdoc />
    public async Task<int> GetFaultCountAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var faults = await _repository.GetFaultsByCorrelationIdAsync(correlationId, cancellationToken)
            .ConfigureAwait(false);

        return faults.Count;
    }
}
