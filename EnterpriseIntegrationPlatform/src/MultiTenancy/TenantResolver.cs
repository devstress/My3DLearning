namespace EnterpriseIntegrationPlatform.MultiTenancy;

/// <summary>
/// Production implementation of <see cref="ITenantResolver"/>.
/// Reads the tenant identifier from the <c>tenantId</c> metadata key on a message envelope.
/// </summary>
public sealed class TenantResolver : ITenantResolver
{
    /// <summary>
    /// The metadata key used to carry the tenant identifier on message envelopes.
    /// </summary>
    public const string TenantMetadataKey = "tenantId";

    /// <inheritdoc />
    public TenantContext Resolve(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (metadata.TryGetValue(TenantMetadataKey, out var tenantId) &&
            !string.IsNullOrWhiteSpace(tenantId))
        {
            return new TenantContext
            {
                TenantId = tenantId,
                IsResolved = true,
            };
        }

        return TenantContext.Anonymous;
    }

    /// <inheritdoc />
    public TenantContext Resolve(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return TenantContext.Anonymous;

        return new TenantContext
        {
            TenantId = tenantId,
            IsResolved = true,
        };
    }
}
