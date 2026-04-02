namespace EnterpriseIntegrationPlatform.Configuration;

/// <summary>
/// Represents a feature flag with optional variant support, rollout percentage, and tenant targeting.
/// </summary>
/// <param name="Name">Unique feature flag name (e.g. "NewCheckoutFlow").</param>
/// <param name="IsEnabled">Master on/off switch for the flag.</param>
/// <param name="Variants">Named variants with their associated values (e.g. "control" → "v1", "treatment" → "v2").</param>
/// <param name="RolloutPercentage">Percentage of traffic (0–100) that should receive this feature.</param>
/// <param name="TargetTenants">Tenants that always receive this feature regardless of rollout percentage.</param>
public sealed record FeatureFlag(
    string Name,
    bool IsEnabled = false,
    Dictionary<string, string>? Variants = null,
    int RolloutPercentage = 100,
    List<string>? TargetTenants = null)
{
    /// <summary>Named variants with their associated values.</summary>
    public Dictionary<string, string> Variants { get; init; } = Variants ?? new();

    /// <summary>Tenants that always receive this feature regardless of rollout percentage.</summary>
    public List<string> TargetTenants { get; init; } = TargetTenants ?? [];
}
