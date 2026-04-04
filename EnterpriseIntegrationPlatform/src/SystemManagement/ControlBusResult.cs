namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Describes the outcome of a <see cref="IControlBus.PublishCommandAsync{T}"/> call.
/// </summary>
/// <param name="Succeeded">Whether the control command was published successfully.</param>
/// <param name="ControlTopic">The topic the command was published to.</param>
/// <param name="FailureReason">Description of the failure, if any.</param>
public sealed record ControlBusResult(
    bool Succeeded,
    string ControlTopic,
    string? FailureReason = null);
