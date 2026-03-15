namespace EnterpriseIntegrationPlatform.Workflow.Temporal;

/// <summary>
/// Configuration options for the Temporal workflow host.
/// Bound from the "Temporal" configuration section.
/// </summary>
public sealed class TemporalOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Temporal";

    /// <summary>Temporal server gRPC address (e.g. "localhost:15233").</summary>
    public string ServerAddress { get; set; } = "localhost:15233";

    /// <summary>Temporal namespace for this worker.</summary>
    public string Namespace { get; set; } = "default";

    /// <summary>Task queue that this worker polls for workflow and activity tasks.</summary>
    public string TaskQueue { get; set; } = "integration-workflows";
}
