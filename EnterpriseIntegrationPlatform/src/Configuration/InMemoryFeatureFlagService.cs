using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.Configuration;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IFeatureFlagService"/>
/// using <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class InMemoryFeatureFlagService : IFeatureFlagService
{
    private readonly ConcurrentDictionary<string, FeatureFlag> _flags = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync(string name, string? tenantId = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!_flags.TryGetValue(name, out var flag) || !flag.IsEnabled)
            return Task.FromResult(false);

        // Targeted tenants always get the feature
        if (tenantId is not null && flag.TargetTenants.Contains(tenantId, StringComparer.OrdinalIgnoreCase))
            return Task.FromResult(true);

        // Check rollout percentage using deterministic hash
        if (flag.RolloutPercentage >= 100)
            return Task.FromResult(true);

        if (flag.RolloutPercentage <= 0)
            return Task.FromResult(false);

        var hashInput = tenantId ?? name;
        var bucket = Math.Abs(hashInput.GetHashCode()) % 100;
        return Task.FromResult(bucket < flag.RolloutPercentage);
    }

    /// <inheritdoc />
    public Task<string?> GetVariantAsync(string name, string variantKey, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(variantKey);

        if (!_flags.TryGetValue(name, out var flag))
            return Task.FromResult<string?>(null);

        flag.Variants.TryGetValue(variantKey, out var value);
        return Task.FromResult(value);
    }

    /// <inheritdoc />
    public Task<FeatureFlag?> GetAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _flags.TryGetValue(name, out var flag);
        return Task.FromResult(flag);
    }

    /// <inheritdoc />
    public Task SetAsync(FeatureFlag flag, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(flag);
        ArgumentException.ThrowIfNullOrWhiteSpace(flag.Name);
        _flags[flag.Name] = flag;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Task.FromResult(_flags.TryRemove(name, out _));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FeatureFlag>> ListAsync(CancellationToken ct = default)
    {
        IReadOnlyList<FeatureFlag> result = _flags.Values.ToList();
        return Task.FromResult(result);
    }
}
