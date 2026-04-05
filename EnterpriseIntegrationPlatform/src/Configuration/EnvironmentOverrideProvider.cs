namespace EnterpriseIntegrationPlatform.Configuration;

/// <summary>
/// Resolves configuration values using environment cascade:
/// specific environment → "default" environment → <c>EIP__</c> environment variables.
/// Supports dev/staging/prod environments with fallback semantics.
/// </summary>
/// <remarks>
/// <para>
/// Environment variables prefixed with <c>EIP__</c> participate in the cascade as
/// the highest-priority override. Double underscore (<c>__</c>) in the variable name
/// maps to <c>:</c> in the configuration key, following the standard .NET convention.
/// For example, the environment variable <c>EIP__Broker__ConnectionString</c>
/// overrides the key <c>Broker:ConnectionString</c>.
/// </para>
/// </remarks>
public sealed class EnvironmentOverrideProvider
{
    /// <summary>Prefix for environment variable overrides.</summary>
    internal const string EnvPrefix = "EIP__";

    private readonly IConfigurationStore _store;

    public EnvironmentOverrideProvider(IConfigurationStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Resolves a configuration value using environment cascade:
    /// <c>EIP__</c> environment variable → specific environment → "default".
    /// </summary>
    /// <param name="key">The configuration key to resolve (e.g. <c>Broker:ConnectionString</c>).</param>
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

        // 1. Highest priority: EIP__ environment variable override.
        var envEntry = ResolveFromEnvironmentVariable(key);
        if (envEntry is not null)
            return envEntry;

        // 2. Try specific environment from store.
        var entry = await _store.GetAsync(key, environment, ct);
        if (entry is not null)
            return entry;

        // 3. Fall back to default environment (skip if already requesting default).
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

    /// <summary>
    /// Checks for an <c>EIP__</c> prefixed environment variable matching the key.
    /// Uses the .NET convention: <c>:</c> in the key maps to <c>__</c> in the variable name.
    /// </summary>
    public static ConfigurationEntry? ResolveFromEnvironmentVariable(string key)
    {
        var envVarName = EnvPrefix + key.Replace(":", "__", StringComparison.Ordinal);
        var value = Environment.GetEnvironmentVariable(envVarName);

        if (value is null)
            return null;

        return new ConfigurationEntry(key, value, "environment-variable");
    }
}
