namespace EnterpriseIntegrationPlatform.Configuration;

/// <summary>
/// Resolves configuration values using environment cascade:
/// specific environment → "default" environment.
/// Supports dev/staging/prod environments with fallback semantics.
/// </summary>
public sealed class EnvironmentOverrideProvider
{
    private readonly IConfigurationStore _store;

    public EnvironmentOverrideProvider(IConfigurationStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Resolves a configuration value using environment cascade.
    /// First checks the specific environment, then falls back to "default".
    /// </summary>
    /// <param name="key">The configuration key to resolve.</param>
    /// <param name="environment">The target environment (e.g. "dev", "staging", "prod").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resolved configuration entry, or null if not found in any cascade level.</returns>
    public async Task<ConfigurationEntry?> ResolveAsync(
        string key,
        string environment,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        // Try specific environment first
        var entry = await _store.GetAsync(key, environment, ct);
        if (entry is not null)
            return entry;

        // Fall back to default environment (skip if already requesting default)
        if (!environment.Equals("default", StringComparison.OrdinalIgnoreCase))
            entry = await _store.GetAsync(key, "default", ct);

        return entry;
    }

    /// <summary>
    /// Resolves multiple configuration keys using environment cascade.
    /// </summary>
    /// <param name="keys">The configuration keys to resolve.</param>
    /// <param name="environment">The target environment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary of resolved key-value pairs (only keys that were found).</returns>
    public async Task<IReadOnlyDictionary<string, ConfigurationEntry>> ResolveManyAsync(
        IEnumerable<string> keys,
        string environment,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        var result = new Dictionary<string, ConfigurationEntry>();

        foreach (var key in keys)
        {
            var entry = await ResolveAsync(key, environment, ct);
            if (entry is not null)
                result[key] = entry;
        }

        return result;
    }
}
