namespace EnterpriseIntegrationPlatform.Configuration;

/// <summary>
/// Represents a single configuration entry with environment-specific overrides.
/// </summary>
/// <param name="Key">Unique configuration key (e.g. "Database:ConnectionString").</param>
/// <param name="Value">The configuration value.</param>
/// <param name="Environment">Target environment (e.g. "dev", "staging", "prod") or "default".</param>
/// <param name="Version">Monotonically increasing version for optimistic concurrency.</param>
/// <param name="LastModified">UTC timestamp of last modification.</param>
/// <param name="ModifiedBy">Identity of the user or service that last modified this entry.</param>
public sealed record ConfigurationEntry(
    string Key,
    string Value,
    string Environment = "default",
    int Version = 1,
    DateTimeOffset LastModified = default,
    string? ModifiedBy = null)
{
    /// <summary>UTC timestamp of last modification, defaults to UtcNow when unset.</summary>
    public DateTimeOffset LastModified { get; init; } =
        LastModified == default ? DateTimeOffset.UtcNow : LastModified;
}
