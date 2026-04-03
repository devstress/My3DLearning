namespace EnterpriseIntegrationPlatform.Processing.RequestReply;

/// <summary>
/// Configuration options for the <see cref="RequestReplyCorrelator{TRequest,TResponse}"/>.
/// </summary>
public sealed class RequestReplyOptions
{
    /// <summary>
    /// Maximum time in milliseconds to wait for a reply before timing out.
    /// Default is 30 000 ms (30 seconds).
    /// </summary>
    public int TimeoutMs { get; set; } = 30_000;

    /// <summary>
    /// Consumer group name used when subscribing to the reply topic.
    /// Default is "request-reply".
    /// </summary>
    public string ConsumerGroup { get; set; } = "request-reply";
}
