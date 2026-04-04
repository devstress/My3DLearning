using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Result of a routing slip step execution.
/// </summary>
/// <param name="StepName">The step that was executed.</param>
/// <param name="Succeeded">Whether the step completed successfully.</param>
/// <param name="FailureReason">Reason for failure, if <paramref name="Succeeded"/> is <c>false</c>.</param>
/// <param name="RemainingSlip">The routing slip after advancing past the executed step.</param>
/// <param name="ForwardedToTopic">
/// The topic the message was forwarded to, or <see langword="null"/> if the next step
/// is executed in-process.
/// </param>
public sealed record RoutingSlipStepResult(
    string StepName,
    bool Succeeded,
    string? FailureReason,
    RoutingSlip RemainingSlip,
    string? ForwardedToTopic);
