namespace EnterpriseIntegrationPlatform.MultiTenancy;

/// <summary>
/// Represents the resolved tenant context for the current operation.
/// Carries the tenant identifier and any associated metadata.
/// </summary>
public sealed class TenantContext
{
    /// <summary>The unique identifier of the tenant.</summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Optional display name of the tenant for logging and diagnostics.
    /// </summary>
    public string? TenantName { get; init; }

    /// <summary>
    /// Indicates whether the context represents a valid, resolved tenant
    /// (<c>true</c>) or an anonymous/unresolved caller (<c>false</c>).
    /// </summary>
    public bool IsResolved { get; init; }

    /// <summary>A pre-built anonymous tenant context used when no tenant can be resolved.</summary>
    public static readonly TenantContext Anonymous = new()
    {
        TenantId = "anonymous",
        IsResolved = false,
    };
}
