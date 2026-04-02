namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Orchestrates the self-service tenant provisioning lifecycle: onboarding,
/// status tracking, and deprovisioning.
/// </summary>
public interface ITenantOnboardingService
{
    /// <summary>
    /// Provisions a new tenant, creating its quota, broker namespace, and
    /// all required platform resources.
    /// </summary>
    /// <param name="request">Onboarding request with tenant details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The onboarding result with status and provisioned resources.</returns>
    Task<TenantOnboardingResult> ProvisionAsync(
        TenantOnboardingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deprovisions an existing tenant, releasing all associated resources.
    /// </summary>
    /// <param name="tenantId">Unique tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated onboarding result reflecting the deprovisioned state.</returns>
    Task<TenantOnboardingResult> DeprovisionAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current onboarding status for a tenant.
    /// </summary>
    /// <param name="tenantId">Unique tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The onboarding result, or <c>null</c> if the tenant is unknown.</returns>
    Task<TenantOnboardingResult?> GetStatusAsync(
        string tenantId,
        CancellationToken cancellationToken = default);
}
