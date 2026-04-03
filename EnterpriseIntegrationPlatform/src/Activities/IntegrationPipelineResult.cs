namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Result of the <c>IntegrationPipelineWorkflow</c>. Indicates whether the full
/// atomic pipeline (persist → validate → deliver/fault → ack/nack) completed
/// successfully.
/// </summary>
/// <param name="MessageId">The processed message identifier.</param>
/// <param name="IsSuccess">Whether the message was delivered successfully.</param>
/// <param name="FailureReason">Reason for failure, if <paramref name="IsSuccess"/> is false.</param>
public sealed record IntegrationPipelineResult(
    Guid MessageId,
    bool IsSuccess,
    string? FailureReason = null);
