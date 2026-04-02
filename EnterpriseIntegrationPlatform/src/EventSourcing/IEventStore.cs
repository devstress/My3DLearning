namespace EnterpriseIntegrationPlatform.EventSourcing;

/// <summary>
/// Append-only event store supporting optimistic concurrency, forward reads, and backward reads.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends one or more events to the specified stream.
    /// </summary>
    /// <param name="streamId">Target event stream identifier.</param>
    /// <param name="events">Events to append. Each event's <see cref="EventEnvelope.Version"/> is set by the store.</param>
    /// <param name="expectedVersion">
    /// The version the caller expects the stream to be at before appending.
    /// Use <c>0</c> for a new stream. A mismatch raises <see cref="OptimisticConcurrencyException"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new stream version after all events have been appended.</returns>
    Task<long> AppendAsync(string streamId, IReadOnlyList<EventEnvelope> events, long expectedVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads events from the specified stream in ascending version order.
    /// </summary>
    /// <param name="streamId">Event stream identifier.</param>
    /// <param name="fromVersion">Inclusive lower-bound version to start reading from.</param>
    /// <param name="count">Maximum number of events to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Events in ascending version order.</returns>
    Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(string streamId, long fromVersion, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads events from the specified stream in descending version order.
    /// </summary>
    /// <param name="streamId">Event stream identifier.</param>
    /// <param name="fromVersion">Inclusive upper-bound version to start reading from.</param>
    /// <param name="count">Maximum number of events to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Events in descending version order.</returns>
    Task<IReadOnlyList<EventEnvelope>> ReadStreamBackwardAsync(string streamId, long fromVersion, int count, CancellationToken cancellationToken = default);
}
