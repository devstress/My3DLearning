using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Publish-Subscribe Channel implementation — wraps <see cref="IMessageBrokerProducer"/>
/// and <see cref="IMessageBrokerConsumer"/> to provide fan-out delivery. Each subscriber
/// gets a unique consumer group so every subscriber receives every message.
/// </summary>
public sealed class PublishSubscribeChannel : IPublishSubscribeChannel
{
    private readonly IMessageBrokerProducer _producer;
    private readonly IMessageBrokerConsumer _consumer;
    private readonly ILogger<PublishSubscribeChannel> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PublishSubscribeChannel"/>.
    /// </summary>
    /// <param name="producer">The message broker producer.</param>
    /// <param name="consumer">The message broker consumer.</param>
    /// <param name="logger">Logger instance.</param>
    public PublishSubscribeChannel(
        IMessageBrokerProducer producer,
        IMessageBrokerConsumer consumer,
        ILogger<PublishSubscribeChannel> logger)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string channel,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);

        _logger.LogDebug("Pub-Sub publish to channel {Channel}, MessageId={MessageId}",
            channel, envelope.MessageId);

        await _producer.PublishAsync(envelope, channel, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(
        string channel,
        string subscriberId,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriberId);
        ArgumentNullException.ThrowIfNull(handler);

        // Each subscriber gets a unique consumer group derived from its subscriberId.
        // This ensures fan-out: the broker delivers every message to every subscriber.
        var consumerGroup = $"pubsub-{channel}-{subscriberId}";

        _logger.LogInformation(
            "Pub-Sub subscribe on channel {Channel}, SubscriberId={SubscriberId}, ConsumerGroup={ConsumerGroup}",
            channel, subscriberId, consumerGroup);

        await _consumer.SubscribeAsync(channel, consumerGroup, handler, cancellationToken);
    }
}
