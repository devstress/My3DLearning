namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Stores and queries <see cref="MessageEvent"/> records that track the lifecycle
/// of messages as they flow through the integration platform.
/// This is the queryable backing store that makes it possible to answer
/// questions like "where is my shipment for order 02?".
/// </summary>
/// <remarks>
/// Implementations may be in-memory (for development / testing),
/// backed by Cassandra (for production), or any other durable store.
/// </remarks>
public interface IMessageStateStore
{
    /// <summary>
    /// Records a lifecycle event for a message.
    /// </summary>
    /// <param name="messageEvent">The event to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all lifecycle events for a given correlation identifier,
    /// ordered by <see cref="MessageEvent.RecordedAt"/> ascending.
    /// </summary>
    /// <param name="correlationId">The correlation identifier to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ordered list of events, or an empty list if none are found.</returns>
    Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all lifecycle events for a given business key
    /// (e.g. order number, shipment ID), ordered by timestamp ascending.
    /// </summary>
    /// <param name="businessKey">The business key to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ordered list of events, or an empty list if none are found.</returns>
    Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all lifecycle events for a given message identifier,
    /// ordered by timestamp ascending.
    /// </summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ordered list of events, or an empty list if none are found.</returns>
    Task<IReadOnlyList<MessageEvent>> GetByMessageIdAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recent event for a given correlation identifier,
    /// representing the current known state of the message.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest event, or <c>null</c> if no events are found.</returns>
    Task<MessageEvent?> GetLatestByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
