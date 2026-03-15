namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Input parameters for the <c>ProcessIntegrationMessageWorkflow</c>.
/// Shared between the workflow worker and any client that dispatches the workflow.
/// </summary>
/// <param name="MessageId">Unique identifier of the message being processed.</param>
/// <param name="MessageType">Logical message type name.</param>
/// <param name="PayloadJson">JSON-serialised payload to validate and route.</param>
public record ProcessIntegrationMessageInput(
    Guid MessageId,
    string MessageType,
    string PayloadJson);
