using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Production implementation of the Channel Purger Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// Drains messages from a topic by subscribing with a short-lived consumer
/// that counts and discards all available messages. The purge runs until
/// no more messages are received within the configured drain timeout.
/// </para>
/// </remarks>
public sealed class ChannelPurger : IChannelPurger
{
    private readonly IMessageBrokerConsumer _consumer;
    private readonly ILogger<ChannelPurger> _logger;
    private readonly TimeSpan _drainTimeout;

    /// <summary>Initialises a new instance of <see cref="ChannelPurger"/>.</summary>
    /// <param name="consumer">The broker consumer used to drain messages.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="drainTimeout">
    /// The time to wait for messages before considering the channel drained.
    /// Default is 2 seconds.
    /// </param>
    public ChannelPurger(
        IMessageBrokerConsumer consumer,
        ILogger<ChannelPurger> logger,
        TimeSpan? drainTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(logger);

        _consumer = consumer;
        _logger = logger;
        _drainTimeout = drainTimeout ?? TimeSpan.FromSeconds(2);
    }

    /// <inheritdoc />
    public async Task<ChannelPurgeResult> PurgeAsync(
        string topic,
        string consumerGroup,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);

        var purgedCount = 0;

        _logger.LogInformation("Channel Purger: starting purge of topic '{Topic}'", topic);

        try
        {
            using var drainCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            drainCts.CancelAfter(_drainTimeout);

            try
            {
                await _consumer.SubscribeAsync<object>(
                    topic,
                    consumerGroup,
                    _ =>
                    {
                        Interlocked.Increment(ref purgedCount);
                        return Task.CompletedTask;
                    },
                    drainCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (drainCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Drain timeout reached — expected behaviour
            }

            _logger.LogInformation(
                "Channel Purger: purged {PurgedCount} messages from topic '{Topic}'",
                purgedCount, topic);

            return new ChannelPurgeResult(topic, purgedCount, Succeeded: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Let external cancellation propagate
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Channel Purger: failed to purge topic '{Topic}' after draining {PurgedCount} messages",
                topic, purgedCount);

            return new ChannelPurgeResult(topic, purgedCount, Succeeded: false,
                FailureReason: ex.Message);
        }
    }
}
