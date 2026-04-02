namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Subscription plan tiers that determine default quotas and isolation levels.
/// </summary>
public enum TenantPlan
{
    /// <summary>Free tier with minimal quotas and shared resources.</summary>
    Free = 0,

    /// <summary>Standard tier with moderate quotas and shared resources.</summary>
    Standard = 1,

    /// <summary>Premium tier with higher quotas and optional dedicated resources.</summary>
    Premium = 2,

    /// <summary>Enterprise tier with maximum quotas and dedicated resources.</summary>
    Enterprise = 3,
}
