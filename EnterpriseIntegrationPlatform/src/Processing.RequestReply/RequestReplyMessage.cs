namespace EnterpriseIntegrationPlatform.Processing.RequestReply;

/// <summary>
/// Describes a request to be sent via the Request-Reply pattern.
/// </summary>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <param name="Payload">The request payload.</param>
/// <param name="RequestTopic">The topic to publish the request to.</param>
/// <param name="ReplyTopic">The topic where the reply is expected.</param>
/// <param name="Source">Source system name for the envelope.</param>
/// <param name="MessageType">Message type for the request envelope.</param>
/// <param name="CorrelationId">
/// Optional correlation identifier. When null, a new one is generated.
/// </param>
public record RequestReplyMessage<TRequest>(
    TRequest Payload,
    string RequestTopic,
    string ReplyTopic,
    string Source,
    string MessageType,
    Guid? CorrelationId = null);
