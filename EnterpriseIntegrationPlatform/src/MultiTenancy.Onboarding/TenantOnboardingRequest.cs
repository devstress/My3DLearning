namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Inbound request for provisioning a new tenant on the platform.
/// </summary>
/// <param name="TenantId">Unique tenant identifier. Must be non-empty.</param>
/// <param name="TenantName">Human-readable tenant display name.</param>
/// <param name="Plan">Subscription plan tier that governs default quotas.</param>
/// <param name="AdminEmail">Contact e-mail for the tenant administrator.</param>
/// <param name="Metadata">Optional key-value metadata attached to the tenant.</param>
public sealed record TenantOnboardingRequest(
    string TenantId,
    string TenantName,
    TenantPlan Plan,
    string AdminEmail,
    IReadOnlyDictionary<string, string>? Metadata = null);
