namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// A single step descriptor within a <see cref="RoutingSlip"/>.
/// Each step identifies a processing handler and an optional destination topic.
/// </summary>
/// <param name="StepName">
/// Unique name identifying the processing handler to invoke for this step
/// (e.g. "Validate", "Transform", "Enrich", "Deliver").
/// </param>
/// <param name="DestinationTopic">
/// Optional topic to forward the message to after this step completes.
/// When <see langword="null"/>, the next step in the slip is executed in-process.
/// </param>
/// <param name="Parameters">
/// Optional key-value parameters consumed by the step handler.
/// </param>
public sealed record RoutingSlipStep(
    string StepName,
    string? DestinationTopic = null,
    IReadOnlyDictionary<string, string>? Parameters = null);
