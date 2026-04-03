using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.RequestReply;

/// <summary>
/// Enterprise Integration Pattern: Request-Reply.
/// Sends a request message with <see cref="IntegrationEnvelope{TRequest}.ReplyTo"/> set,
/// subscribes to the reply topic, and correlates the response by <see cref="IntegrationEnvelope{T}.CorrelationId"/>
/// with a configurable timeout.
/// </summary>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <typeparam name="TResponse">The type of the expected response payload.</typeparam>
public interface IRequestReplyCorrelator<TRequest, TResponse>
{
    /// <summary>
    /// Sends a request message to the specified topic and waits for a correlated reply.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The correlated reply result, which may indicate timeout.</returns>
    Task<RequestReplyResult<TResponse>> SendAndReceiveAsync(
        RequestReplyMessage<TRequest> request,
        CancellationToken cancellationToken = default);
}
