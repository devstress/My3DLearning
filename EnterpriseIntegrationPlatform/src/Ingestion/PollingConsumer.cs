using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Polling consumer implementation that wraps <see cref="IMessageBrokerConsumer"/>
/// with pull-based batch retrieval semantics. Suitable for Kafka's pull-based model
/// where the consumer controls the pace of message retrieval.
/// Thread-safe.
/// </summary>
public sealed class PollingConsumer : IPollingConsumer
{
    private readonly IMessageBrokerConsumer _consumer;
    private readonly ILogger<PollingConsumer> _logger;
    private readonly TimeSpan _pollTimeout;

    /// <summary>
    /// Initializes a new instance of <see cref="PollingConsumer"/>.
    /// </summary>
    /// <param name="consumer">The underlying broker consumer.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="pollTimeout">Maximum time to wait for messages per poll. Defaults to 1 second.</param>
    public PollingConsumer(
        IMessageBrokerConsumer consumer,
        ILogger<PollingConsumer> logger,
        TimeSpan? pollTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(logger);

        _consumer = consumer;
        _logger = logger;
        _pollTimeout = pollTimeout ?? TimeSpan.FromSeconds(1);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IntegrationEnvelope<T>>> PollAsync<T>(
        string topic,
        string consumerGroup,
        int maxMessages = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);

        if (maxMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMessages), "maxMessages must be greater than zero.");
        }

        var messages = new List<IntegrationEnvelope<T>>();
        using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        pollCts.CancelAfter(_pollTimeout);

        try
        {
            await _consumer.SubscribeAsync<T>(
                topic,
                consumerGroup,
                async envelope =>
                {
                    messages.Add(envelope);
                    if (messages.Count >= maxMessages)
                    {
                        await pollCts.CancelAsync();
                    }
                },
                pollCts.Token);
        }
        catch (OperationCanceledException) when (pollCts.IsCancellationRequested)
        {
            // Expected: poll timeout or maxMessages reached
        }

        _logger.LogDebug(
            "Polled {Count} messages from {Topic}/{Group}",
            messages.Count,
            topic,
            consumerGroup);

        return messages;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _consumer.DisposeAsync();
}
