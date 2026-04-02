namespace EnterpriseIntegrationPlatform.Configuration;

/// <summary>
/// Describes a change to a configuration entry for pub/sub notifications.
/// </summary>
/// <param name="Key">The configuration key that changed.</param>
/// <param name="Environment">The environment in which the change occurred.</param>
/// <param name="ChangeType">The type of change (Created, Updated, Deleted).</param>
/// <param name="OldValue">Previous value, or null if newly created.</param>
/// <param name="NewValue">New value, or null if deleted.</param>
/// <param name="Timestamp">When the change occurred (UTC).</param>
public sealed record ConfigurationChange(
    string Key,
    string Environment,
    ConfigurationChangeType ChangeType,
    string? OldValue,
    string? NewValue,
    DateTimeOffset Timestamp);

/// <summary>Type of configuration change.</summary>
public enum ConfigurationChangeType
{
    /// <summary>A new configuration entry was created.</summary>
    Created,

    /// <summary>An existing configuration entry was updated.</summary>
    Updated,

    /// <summary>A configuration entry was deleted.</summary>
    Deleted
}
