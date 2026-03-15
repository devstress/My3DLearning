namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Result of executing the <c>ProcessIntegrationMessageWorkflow</c>.
/// Shared between the workflow worker and any client that dispatches the workflow.
/// </summary>
/// <param name="MessageId">The message that was processed.</param>
/// <param name="IsValid">Whether the message passed validation.</param>
/// <param name="FailureReason">Reason for failure, if any.</param>
public record ProcessIntegrationMessageResult(
    Guid MessageId,
    bool IsValid,
    string? FailureReason = null);
