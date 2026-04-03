using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.DeadLetter;

/// <summary>
/// Checks whether an <see cref="IntegrationEnvelope{T}"/> has expired (Message Expiration pattern).
/// Expired messages are routed to the Dead Letter Queue with reason <see cref="DeadLetterReason.MessageExpired"/>.
/// </summary>
/// <typeparam name="T">The payload type of the envelope.</typeparam>
public interface IMessageExpirationChecker<T>
{
    /// <summary>
    /// Checks the <see cref="IntegrationEnvelope{T}.ExpiresAt"/> field.
    /// If the message is expired, publishes it to the DLQ and returns <c>true</c>.
    /// If the message is not expired or has no expiry, returns <c>false</c>.
    /// </summary>
    /// <param name="envelope">The message envelope to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the message was expired and routed to DLQ; <c>false</c> otherwise.</returns>
    Task<bool> CheckAndRouteIfExpiredAsync(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
