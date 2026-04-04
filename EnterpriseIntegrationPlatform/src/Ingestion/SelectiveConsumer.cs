using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Selective consumer implementation that wraps <see cref="IMessageBrokerConsumer"/>
/// with a predicate filter. Only messages matching the predicate are forwarded to the
/// handler; non-matching messages are silently skipped. Thread-safe.
/// </summary>
public sealed class SelectiveConsumer : ISelectiveConsumer
{
    private readonly IMessageBrokerConsumer _consumer;
    private readonly ILogger<SelectiveConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SelectiveConsumer"/>.
    /// </summary>
    /// <param name="consumer">The underlying broker consumer.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public SelectiveConsumer(
        IMessageBrokerConsumer consumer,
        ILogger<SelectiveConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(logger);

        _consumer = consumer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, bool> predicate,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);

        _logger.LogInformation(
            "Starting selective consumer on {Topic}/{Group}",
            topic,
            consumerGroup);

        await _consumer.SubscribeAsync<T>(
            topic,
            consumerGroup,
            async envelope =>
            {
                if (predicate(envelope))
                {
                    _logger.LogDebug(
                        "Message {MessageId} matched selective filter on {Topic}",
                        envelope.MessageId,
                        topic);

                    await handler(envelope);
                }
                else
                {
                    _logger.LogDebug(
                        "Message {MessageId} skipped by selective filter on {Topic}",
                        envelope.MessageId,
                        topic);
                }
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _consumer.DisposeAsync();
}
