using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Processing.DeadLetter;

/// <summary>
/// Production implementation of the Message Expiration check.
/// When <see cref="IntegrationEnvelope{T}.ExpiresAt"/> is set and the current UTC time
/// is past the expiry, the message is published to the Dead Letter Queue.
/// </summary>
/// <typeparam name="T">The payload type of the envelope.</typeparam>
public sealed class MessageExpirationChecker<T> : IMessageExpirationChecker<T>
{
    private readonly IDeadLetterPublisher<T> _deadLetterPublisher;
    private readonly ILogger<MessageExpirationChecker<T>> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initialises a new instance of <see cref="MessageExpirationChecker{T}"/>.</summary>
    public MessageExpirationChecker(
        IDeadLetterPublisher<T> deadLetterPublisher,
        ILogger<MessageExpirationChecker<T>> logger,
        TimeProvider timeProvider)
    {
        _deadLetterPublisher = deadLetterPublisher;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<bool> CheckAndRouteIfExpiredAsync(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!envelope.ExpiresAt.HasValue)
            return false;

        var now = _timeProvider.GetUtcNow();

        if (now <= envelope.ExpiresAt.Value)
            return false;

        _logger.LogWarning(
            "Message {MessageId} (type={MessageType}) expired at {ExpiresAt}. " +
            "Current time: {Now}. Routing to Dead Letter Queue.",
            envelope.MessageId, envelope.MessageType, envelope.ExpiresAt.Value, now);

        await _deadLetterPublisher.PublishAsync(
            envelope,
            DeadLetterReason.MessageExpired,
            $"Message expired at {envelope.ExpiresAt.Value:O}. Current time: {now:O}.",
            attemptCount: 0,
            cancellationToken);

        return true;
    }
}
