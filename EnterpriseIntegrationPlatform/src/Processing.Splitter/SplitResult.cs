using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Splitter;

/// <summary>
/// The result of a <see cref="IMessageSplitter{T}"/> split operation.
/// </summary>
/// <typeparam name="T">Payload type of the split envelopes.</typeparam>
/// <param name="SplitEnvelopes">The individual envelopes that were published.</param>
/// <param name="SourceMessageId">The <see cref="IntegrationEnvelope{T}.MessageId"/> of the source envelope.</param>
/// <param name="TargetTopic">The topic to which the split envelopes were published.</param>
/// <param name="ItemCount">The number of individual items produced by the split.</param>
public sealed record SplitResult<T>(
    IReadOnlyList<IntegrationEnvelope<T>> SplitEnvelopes,
    Guid SourceMessageId,
    string TargetTopic,
    int ItemCount);
