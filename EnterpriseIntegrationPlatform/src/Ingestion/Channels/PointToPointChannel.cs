using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Point-to-Point Channel implementation — wraps <see cref="IMessageBrokerProducer"/>
/// and <see cref="IMessageBrokerConsumer"/> to enforce queue-group semantics where
/// each message is delivered to exactly one consumer in the group.
/// </summary>
public sealed class PointToPointChannel : IPointToPointChannel
{
    private readonly IMessageBrokerProducer _producer;
    private readonly IMessageBrokerConsumer _consumer;
    private readonly ILogger<PointToPointChannel> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PointToPointChannel"/>.
    /// </summary>
    /// <param name="producer">The message broker producer.</param>
    /// <param name="consumer">The message broker consumer.</param>
    /// <param name="logger">Logger instance.</param>
    public PointToPointChannel(
        IMessageBrokerProducer producer,
        IMessageBrokerConsumer consumer,
        ILogger<PointToPointChannel> logger)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        string channel,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);

        _logger.LogDebug("Point-to-Point send to channel {Channel}, MessageId={MessageId}",
            channel, envelope.MessageId);

        await _producer.PublishAsync(envelope, channel, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReceiveAsync<T>(
        string channel,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);
        ArgumentNullException.ThrowIfNull(handler);

        _logger.LogInformation(
            "Point-to-Point subscribe on channel {Channel}, ConsumerGroup={ConsumerGroup}",
            channel, consumerGroup);

        await _consumer.SubscribeAsync(channel, consumerGroup, handler, cancellationToken);
    }
}
