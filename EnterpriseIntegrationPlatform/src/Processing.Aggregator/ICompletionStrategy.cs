using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// Determines whether a correlation group of messages is ready to be aggregated
/// and released as a single composite message.
/// </summary>
/// <typeparam name="T">The payload type of the individual messages.</typeparam>
public interface ICompletionStrategy<T>
{
    /// <summary>
    /// Returns <see langword="true"/> when the <paramref name="group"/> is ready
    /// to be aggregated; <see langword="false"/> if more messages are expected.
    /// </summary>
    /// <param name="group">
    /// The current set of envelopes received for this correlation group,
    /// ordered by arrival time.
    /// </param>
    bool IsComplete(IReadOnlyList<IntegrationEnvelope<T>> group);
}
