namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Result of a tenant onboarding workflow, including provisioned resources and current status.
/// </summary>
/// <param name="TenantId">The provisioned tenant identifier.</param>
/// <param name="Status">Current lifecycle status of the onboarding workflow.</param>
/// <param name="ProvisionedAt">UTC timestamp when provisioning completed (null if not yet active).</param>
/// <param name="NamespaceConfig">Broker namespace configuration assigned to the tenant.</param>
/// <param name="Quota">Resource quotas assigned to the tenant.</param>
/// <param name="ErrorMessage">Error description when <paramref name="Status"/> is <see cref="OnboardingStatus.Failed"/>.</param>
public sealed record TenantOnboardingResult(
    string TenantId,
    OnboardingStatus Status,
    DateTimeOffset? ProvisionedAt,
    BrokerNamespaceConfig? NamespaceConfig,
    TenantQuota? Quota,
    string? ErrorMessage = null);
