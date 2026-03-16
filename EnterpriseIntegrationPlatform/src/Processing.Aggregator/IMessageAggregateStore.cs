using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// Persists the in-flight envelopes for each correlation group until the group
/// is complete and aggregated.
/// </summary>
/// <typeparam name="T">The payload type of the individual messages.</typeparam>
public interface IMessageAggregateStore<T>
{
    /// <summary>
    /// Appends <paramref name="envelope"/> to its correlation group and returns the
    /// updated group (all envelopes received so far, ordered by arrival).
    /// </summary>
    /// <param name="envelope">The envelope to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The current, immutable snapshot of the group including the newly added envelope.
    /// </returns>
    Task<IReadOnlyList<IntegrationEnvelope<T>>> AddAsync(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all envelopes for the specified <paramref name="correlationId"/> from
    /// the store. Called after a group has been aggregated and published.
    /// </summary>
    /// <param name="correlationId">The correlation group identifier to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveGroupAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
