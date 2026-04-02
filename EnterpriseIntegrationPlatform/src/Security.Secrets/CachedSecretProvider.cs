using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Decorator that adds in-memory caching with configurable TTL to any <see cref="ISecretProvider"/>.
/// Cache entries are evicted when they exceed the configured time-to-live.
/// Thread-safe via <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class CachedSecretProvider : ISecretProvider
{
    private readonly ISecretProvider _inner;
    private readonly SecretAuditLogger? _auditLogger;
    private readonly TimeSpan _ttl;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    /// <summary>
    /// Initializes a new instance of <see cref="CachedSecretProvider"/>.
    /// </summary>
    /// <param name="inner">The underlying secret provider to cache.</param>
    /// <param name="ttl">Time-to-live for cache entries.</param>
    /// <param name="auditLogger">Optional audit logger for recording cache events.</param>
    public CachedSecretProvider(ISecretProvider inner, TimeSpan ttl, SecretAuditLogger? auditLogger = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "Cache TTL must be positive.");
        }

        _inner = inner;
        _ttl = ttl;
        _auditLogger = auditLogger;
    }

    /// <inheritdoc />
    public async Task<SecretEntry?> GetSecretAsync(string key, string? version = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var cacheKey = BuildCacheKey(key, version);

        if (_cache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
        {
            _auditLogger?.LogCacheHit(key);
            return cached.Entry;
        }

        if (cached is not null)
        {
            _cache.TryRemove(cacheKey, out _);
            _auditLogger?.LogCacheEvict(key, "TTL expired");
        }

        var entry = await _inner.GetSecretAsync(key, version, ct);
        if (entry is not null)
        {
            _cache[cacheKey] = new CacheEntry(entry, DateTimeOffset.UtcNow.Add(_ttl));
        }

        return entry;
    }

    /// <inheritdoc />
    public async Task<SecretEntry> SetSecretAsync(string key, string value, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        var entry = await _inner.SetSecretAsync(key, value, metadata, ct);
        InvalidateKey(key);
        return entry;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSecretAsync(string key, CancellationToken ct = default)
    {
        var result = await _inner.DeleteSecretAsync(key, ct);
        InvalidateKey(key);
        return result;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListSecretKeysAsync(string? prefix = null, CancellationToken ct = default)
    {
        return _inner.ListSecretKeysAsync(prefix, ct);
    }

    /// <summary>
    /// Removes all cached entries whose keys start with the given prefix.
    /// When no prefix is provided, the entire cache is cleared.
    /// </summary>
    /// <param name="prefix">Optional key prefix to invalidate.</param>
    public void Invalidate(string? prefix = null)
    {
        if (prefix is null)
        {
            _cache.Clear();
            return;
        }

        foreach (var cacheKey in _cache.Keys)
        {
            if (cacheKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                _cache.TryRemove(cacheKey, out _);
                _auditLogger?.LogCacheEvict(cacheKey, "Manual invalidation");
            }
        }
    }

    private void InvalidateKey(string key)
    {
        var keysToRemove = _cache.Keys
            .Where(k => k.StartsWith(key, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var k in keysToRemove)
        {
            _cache.TryRemove(k, out _);
            _auditLogger?.LogCacheEvict(key, "Write-through invalidation");
        }
    }

    private static string BuildCacheKey(string key, string? version) =>
        version is not null ? $"{key}::{version}" : key;

    private sealed record CacheEntry(SecretEntry Entry, DateTimeOffset ExpiresAt)
    {
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    }
}
