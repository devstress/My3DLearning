using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Durable subscriber implementation that wraps <see cref="IMessageBrokerConsumer"/>
/// with durable subscription semantics. The subscription state (identified by name)
/// survives consumer restarts — the broker retains undelivered messages and delivers
/// them when the consumer reconnects. This is inherent in Kafka consumer groups,
/// NATS JetStream durable consumers, and Pulsar durable subscriptions.
/// Thread-safe.
/// </summary>
public sealed class DurableSubscriber : IDurableSubscriber
{
    private readonly IMessageBrokerConsumer _consumer;
    private readonly ILogger<DurableSubscriber> _logger;
    private volatile bool _isConnected;

    /// <summary>
    /// Initializes a new instance of <see cref="DurableSubscriber"/>.
    /// </summary>
    /// <param name="consumer">The underlying broker consumer.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public DurableSubscriber(
        IMessageBrokerConsumer consumer,
        ILogger<DurableSubscriber> logger)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(logger);

        _consumer = consumer;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsConnected => _isConnected;

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(
        string topic,
        string subscriptionName,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
        ArgumentNullException.ThrowIfNull(handler);

        _logger.LogInformation(
            "Starting durable subscription '{SubscriptionName}' on {Topic}",
            subscriptionName,
            topic);

        _isConnected = true;

        try
        {
            await _consumer.SubscribeAsync(
                topic,
                subscriptionName,
                handler,
                cancellationToken);
        }
        finally
        {
            _isConnected = false;
            _logger.LogInformation(
                "Durable subscription '{SubscriptionName}' on {Topic} disconnected",
                subscriptionName,
                topic);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _isConnected = false;
        await _consumer.DisposeAsync();
    }
}
