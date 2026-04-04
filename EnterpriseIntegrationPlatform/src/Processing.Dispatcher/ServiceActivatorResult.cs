namespace EnterpriseIntegrationPlatform.Processing.Dispatcher;

/// <summary>
/// Describes the outcome of a <see cref="IServiceActivator"/> invocation.
/// </summary>
/// <param name="Succeeded">Whether the service operation completed without error.</param>
/// <param name="ReplySent">Whether a reply was published to the <c>ReplyTo</c> address.</param>
/// <param name="ReplyTopic">The topic the reply was published to, if any.</param>
/// <param name="FailureReason">Description of the failure, if any.</param>
public sealed record ServiceActivatorResult(
    bool Succeeded,
    bool ReplySent,
    string? ReplyTopic = null,
    string? FailureReason = null);
