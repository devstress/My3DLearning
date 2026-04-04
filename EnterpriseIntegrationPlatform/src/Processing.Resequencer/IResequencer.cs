using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Resequencer;

/// <summary>
/// Buffers out-of-order messages by <c>CorrelationId</c> + <c>SequenceNumber</c>,
/// and releases them in order once the sequence is complete or a configurable
/// timeout expires. This is the Enterprise Integration Patterns "Resequencer" pattern.
/// </summary>
public interface IResequencer
{
    /// <summary>
    /// Accepts a message with sequence information. If the message completes a sequence
    /// or fills a gap, all ready messages are released in order. If the message is
    /// out-of-order, it is buffered until predecessors arrive or the timeout expires.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">
    /// The message envelope. Must have <c>SequenceNumber</c> and <c>TotalCount</c> set.
    /// </param>
    /// <returns>
    /// An ordered list of messages ready for delivery. Empty if the message was buffered.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="envelope"/> does not have sequence information.
    /// </exception>
    IReadOnlyList<IntegrationEnvelope<T>> Accept<T>(IntegrationEnvelope<T> envelope);

    /// <summary>
    /// Releases all buffered messages for the given <paramref name="correlationId"/>
    /// in their current order, regardless of completeness. Used when a timeout expires.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="correlationId">The correlation ID of the sequence to release.</param>
    /// <returns>The buffered messages in sequence order.</returns>
    IReadOnlyList<IntegrationEnvelope<T>> ReleaseOnTimeout<T>(Guid correlationId);

    /// <summary>
    /// Returns the number of sequences currently being buffered.
    /// </summary>
    int ActiveSequenceCount { get; }
}
