namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Lifecycle status of a tenant onboarding workflow.
/// </summary>
public enum OnboardingStatus
{
    /// <summary>The onboarding request has been received but not yet started.</summary>
    Pending = 0,

    /// <summary>Resources are being provisioned for the tenant.</summary>
    Provisioning = 1,

    /// <summary>The tenant is fully provisioned and active.</summary>
    Active = 2,

    /// <summary>Provisioning failed; see <see cref="TenantOnboardingResult.ErrorMessage"/>.</summary>
    Failed = 3,

    /// <summary>The tenant has been deprovisioned and all resources released.</summary>
    Deprovisioned = 4,
}
