namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Configuration options for the <see cref="Detour"/> routing component.
/// </summary>
public sealed class DetourOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Detour";

    /// <summary>
    /// The topic that messages are routed to when the detour is active.
    /// </summary>
    public required string DetourTopic { get; set; }

    /// <summary>
    /// The topic that messages are routed to when the detour is inactive
    /// (normal processing).
    /// </summary>
    public required string OutputTopic { get; set; }

    /// <summary>
    /// When <c>true</c>, the detour is enabled at startup.
    /// Default is <c>false</c>.
    /// </summary>
    public bool EnabledAtStartup { get; set; }

    /// <summary>
    /// Optional metadata key to check for per-message detour activation.
    /// When set, any message with this metadata key set to <c>"true"</c>
    /// will be detoured regardless of the global switch.
    /// Default is <c>null</c> (no per-message check).
    /// </summary>
    public string? DetourMetadataKey { get; set; }
}
