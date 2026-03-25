namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Input parameters for the <c>SagaCompensationWorkflow</c>.
/// Carries the correlation identifier and the ordered list of compensation step names
/// that must be executed in reverse order (last-to-first) to undo committed work.
/// </summary>
/// <param name="CorrelationId">Correlation ID of the saga that failed.</param>
/// <param name="OriginalMessageId">Message ID of the message that triggered the saga.</param>
/// <param name="MessageType">Logical message type for logging and routing.</param>
/// <param name="CompensationSteps">
/// Ordered list of compensation step names to execute (in forward order;
/// the workflow reverses them before running).
/// </param>
/// <param name="FailureReason">Human-readable description of why compensation was triggered.</param>
public record SagaCompensationInput(
    Guid CorrelationId,
    Guid OriginalMessageId,
    string MessageType,
    IReadOnlyList<string> CompensationSteps,
    string FailureReason);
