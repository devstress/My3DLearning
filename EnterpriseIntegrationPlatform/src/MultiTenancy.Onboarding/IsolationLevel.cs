namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Broker namespace isolation level for a tenant.
/// </summary>
public enum IsolationLevel
{
    /// <summary>Tenant shares broker infrastructure with other tenants.</summary>
    Shared = 0,

    /// <summary>Tenant has a dedicated, isolated broker namespace.</summary>
    Dedicated = 1,
}
