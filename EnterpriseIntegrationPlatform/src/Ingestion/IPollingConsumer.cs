using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Polling Consumer — actively polls the message broker for new messages at a configurable
/// interval. Suitable for batch-oriented processing where the consumer controls the pace
/// of message retrieval. Maps to Kafka's pull-based consumption model.
///
/// <para>
/// EIP Pattern: <c>Polling Consumer</c> (Chapter 10, p. 494 of Enterprise Integration Patterns).
/// </para>
/// </summary>
public interface IPollingConsumer : IAsyncDisposable
{
    /// <summary>
    /// Polls the specified topic for the next batch of messages. Returns an empty
    /// collection when no messages are available within the poll timeout.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="topic">Topic or subject to poll.</param>
    /// <param name="consumerGroup">Logical consumer group name.</param>
    /// <param name="maxMessages">Maximum number of messages to return per poll.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A batch of messages retrieved from the topic.</returns>
    Task<IReadOnlyList<IntegrationEnvelope<T>>> PollAsync<T>(
        string topic,
        string consumerGroup,
        int maxMessages = 10,
        CancellationToken cancellationToken = default);
}
