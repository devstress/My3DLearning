namespace EnterpriseIntegrationPlatform.Connectors;

/// <summary>
/// Describes a registered connector instance in the platform.
/// </summary>
public sealed record ConnectorDescriptor
{
    /// <summary>
    /// Unique name used to look up this connector (e.g. "order-api", "sftp-vendor-a").
    /// Case-insensitive.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>The transport type of this connector.</summary>
    public required ConnectorType ConnectorType { get; init; }

    /// <summary>
    /// Whether this connector is currently enabled. Disabled connectors are skipped
    /// during resolution.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Optional human-readable description for diagnostics and admin display.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The CLR type of the concrete connector implementation, populated at registration time.
    /// </summary>
    public required Type ImplementationType { get; init; }
}
