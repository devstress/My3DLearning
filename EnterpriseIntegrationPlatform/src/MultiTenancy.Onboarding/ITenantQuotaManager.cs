namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Manages per-tenant resource quotas and enforces usage limits.
/// </summary>
public interface ITenantQuotaManager
{
    /// <summary>
    /// Retrieves the current quota for a tenant.
    /// </summary>
    /// <param name="tenantId">Unique tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant quota, or <c>null</c> if no quota is assigned.</returns>
    Task<TenantQuota?> GetQuotaAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates the quota for a tenant.
    /// </summary>
    /// <param name="tenantId">Unique tenant identifier.</param>
    /// <param name="quota">The quota to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetQuotaAsync(
        string tenantId,
        TenantQuota quota,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a specific resource usage is within the tenant's quota.
    /// </summary>
    /// <param name="tenantId">Unique tenant identifier.</param>
    /// <param name="resourceType">The resource being consumed (e.g. "messages", "storage").</param>
    /// <param name="usage">Current usage amount.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the usage is within quota; <c>false</c> if the quota is exceeded.</returns>
    Task<bool> EnforceQuotaAsync(
        string tenantId,
        string resourceType,
        long usage,
        CancellationToken cancellationToken = default);
}
