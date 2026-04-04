namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Result of a successful Smart Proxy reply correlation.
/// </summary>
/// <param name="CorrelationId">The correlation identifier that linked request and reply.</param>
/// <param name="OriginalReplyTo">The original requester's reply-to address to forward the reply to.</param>
/// <param name="RequestMessageId">The message identifier of the original request.</param>
public sealed record SmartProxyCorrelation(
    Guid CorrelationId,
    string OriginalReplyTo,
    Guid RequestMessageId);
