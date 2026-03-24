namespace EnterpriseIntegrationPlatform.MultiTenancy;

/// <summary>
/// Resolves the <see cref="TenantContext"/> for the current operation from a message
/// or from ambient context (e.g. HTTP headers, JWT claims, message metadata).
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves the tenant from metadata key-value pairs on a message envelope.
    /// Returns <see cref="TenantContext.Anonymous"/> when no tenant key is present.
    /// </summary>
    /// <param name="metadata">Message metadata dictionary.</param>
    TenantContext Resolve(IReadOnlyDictionary<string, string> metadata);

    /// <summary>
    /// Resolves the tenant from a raw tenant ID string.
    /// Returns <see cref="TenantContext.Anonymous"/> when the string is null or whitespace.
    /// </summary>
    /// <param name="tenantId">A raw tenant identifier string.</param>
    TenantContext Resolve(string? tenantId);
}
