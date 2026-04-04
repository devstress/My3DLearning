namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Describes the outcome of a <see cref="ITestMessageGenerator"/> operation.
/// </summary>
/// <param name="MessageId">The unique identifier of the generated test message.</param>
/// <param name="TargetTopic">The topic the test message was published to.</param>
/// <param name="Succeeded">Whether the test message was published successfully.</param>
/// <param name="FailureReason">Description of the failure, if any.</param>
public sealed record TestMessageResult(
    Guid MessageId,
    string TargetTopic,
    bool Succeeded,
    string? FailureReason = null);
