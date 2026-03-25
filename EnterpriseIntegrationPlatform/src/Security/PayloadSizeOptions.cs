namespace EnterpriseIntegrationPlatform.Security;

/// <summary>
/// Configuration options for payload size enforcement.
/// Bind from the <c>PayloadSize</c> configuration section.
/// </summary>
public sealed class PayloadSizeOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "PayloadSize";

    /// <summary>
    /// Maximum allowed payload size in bytes. Defaults to 1 MB (1,048,576 bytes).
    /// </summary>
    public int MaxPayloadBytes { get; set; } = 1_048_576;
}
