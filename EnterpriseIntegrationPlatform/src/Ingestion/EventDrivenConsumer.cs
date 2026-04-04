using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Event-driven consumer implementation that wraps <see cref="IMessageBrokerConsumer"/>
/// with push-based subscription semantics. The broker pushes messages to the consumer
/// automatically. Suitable for NATS JetStream push subscriptions and Pulsar consumers.
/// Thread-safe.
/// </summary>
public sealed class EventDrivenConsumer : IEventDrivenConsumer
{
    private readonly IMessageBrokerConsumer _consumer;
    private readonly ILogger<EventDrivenConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EventDrivenConsumer"/>.
    /// </summary>
    /// <param name="consumer">The underlying broker consumer.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public EventDrivenConsumer(
        IMessageBrokerConsumer consumer,
        ILogger<EventDrivenConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(logger);

        _consumer = consumer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);
        ArgumentNullException.ThrowIfNull(handler);

        _logger.LogInformation(
            "Starting event-driven consumer on {Topic}/{Group}",
            topic,
            consumerGroup);

        await _consumer.SubscribeAsync(topic, consumerGroup, handler, cancellationToken);

        _logger.LogInformation(
            "Event-driven consumer stopped on {Topic}/{Group}",
            topic,
            consumerGroup);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _consumer.DisposeAsync();
}
