using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Storage.Cassandra;

/// <summary>
/// Repository interface for persisting and querying messages and faults
/// in the Cassandra distributed store. This is the platform's durable
/// persistence layer — all accepted messages are persisted here to satisfy
/// the Zero Message Loss guarantee (Quality Pillar 1 – Reliability).
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Persists a message record to Cassandra (written to both
    /// <c>messages_by_correlation_id</c> and <c>messages_by_id</c> tables).
    /// </summary>
    /// <param name="record">The message record to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveMessageAsync(MessageRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a fault envelope to the <c>faults_by_correlation_id</c> table.
    /// </summary>
    /// <param name="fault">The fault to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveFaultAsync(FaultEnvelope fault, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all message records for a given correlation identifier,
    /// ordered by <see cref="MessageRecord.RecordedAt"/> ascending.
    /// </summary>
    /// <param name="correlationId">The correlation identifier to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ordered list of records, or an empty list if none are found.</returns>
    Task<IReadOnlyList<MessageRecord>> GetByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the message record for a specific message identifier.
    /// </summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The record, or <c>null</c> if not found.</returns>
    Task<MessageRecord?> GetByMessageIdAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all fault envelopes for a given correlation identifier,
    /// ordered by <see cref="FaultEnvelope.FaultedAt"/> descending.
    /// </summary>
    /// <param name="correlationId">The correlation identifier to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ordered list of faults, or an empty list if none are found.</returns>
    Task<IReadOnlyList<FaultEnvelope>> GetFaultsByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the delivery status of a message in both tables.
    /// </summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="correlationId">The correlation identifier (needed for the partition key).</param>
    /// <param name="recordedAt">The original recorded-at timestamp (needed for the clustering key).</param>
    /// <param name="status">The new delivery status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateDeliveryStatusAsync(
        Guid messageId,
        Guid correlationId,
        DateTimeOffset recordedAt,
        DeliveryStatus status,
        CancellationToken cancellationToken = default);
}
