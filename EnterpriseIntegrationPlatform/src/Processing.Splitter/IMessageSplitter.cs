using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Splitter;

/// <summary>
/// Implements the Splitter Enterprise Integration Pattern.
/// Splits a composite <see cref="IntegrationEnvelope{T}"/> into individual
/// <see cref="IntegrationEnvelope{T}"/> messages and publishes each to the
/// configured target topic.
/// </summary>
/// <typeparam name="T">The payload type of the envelope.</typeparam>
public interface IMessageSplitter<T>
{
    /// <summary>
    /// Splits the <paramref name="source"/> envelope into individual envelopes,
    /// publishes each to the configured target topic, and returns the split result.
    /// </summary>
    /// <param name="source">The composite envelope to split.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="SplitResult{T}"/> containing the individual envelopes,
    /// the source message identifier, the target topic, and the item count.
    /// </returns>
    Task<SplitResult<T>> SplitAsync(
        IntegrationEnvelope<T> source,
        CancellationToken cancellationToken = default);
}
