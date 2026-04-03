using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.RequestReply;

/// <summary>
/// Result of a Request-Reply operation.
/// </summary>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
/// <param name="CorrelationId">The correlation identifier that linked request and reply.</param>
/// <param name="Reply">The reply envelope, or null if the operation timed out.</param>
/// <param name="TimedOut">Whether the operation timed out before a reply was received.</param>
/// <param name="Duration">Elapsed time of the request-reply operation.</param>
public record RequestReplyResult<TResponse>(
    Guid CorrelationId,
    IntegrationEnvelope<TResponse>? Reply,
    bool TimedOut,
    TimeSpan Duration);
