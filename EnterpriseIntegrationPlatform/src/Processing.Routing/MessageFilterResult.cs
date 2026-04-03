namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Describes the outcome of a message filter evaluation.
/// </summary>
/// <param name="Passed">
/// <see langword="true"/> when the message passed the predicate and was published to the output topic;
/// <see langword="false"/> when it was discarded.
/// </param>
/// <param name="OutputTopic">
/// The topic the message was published to. Either the configured output topic (when passed)
/// or the discard topic (when discarded and a discard topic is configured), or
/// <see langword="null"/> when silently discarded.
/// </param>
/// <param name="Reason">
/// Human-readable reason for the decision. Contains the discard reason when the message
/// did not pass.
/// </param>
public sealed record MessageFilterResult(
    bool Passed,
    string? OutputTopic,
    string Reason);
