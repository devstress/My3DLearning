using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Caching decorator for <see cref="IEnrichmentSource"/>. Wraps an inner source
/// and caches results in <see cref="IMemoryCache"/> with a configurable TTL.
/// </summary>
public sealed class CachedEnrichmentSource : IEnrichmentSource
{
    private readonly IEnrichmentSource _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;
    private readonly ILogger<CachedEnrichmentSource> _logger;

    /// <summary>Initialises a new caching decorator.</summary>
    /// <param name="inner">The underlying enrichment source.</param>
    /// <param name="cache">In-memory cache.</param>
    /// <param name="ttl">Time-to-live for cached entries.</param>
    /// <param name="logger">Logger instance.</param>
    public CachedEnrichmentSource(
        IEnrichmentSource inner,
        IMemoryCache cache,
        TimeSpan ttl,
        ILogger<CachedEnrichmentSource> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _cache = cache;
        _ttl = ttl;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<JsonNode?> FetchAsync(string lookupKey, CancellationToken ct = default)
    {
        var cacheKey = $"enrichment:{lookupKey}";

        if (_cache.TryGetValue(cacheKey, out string? cachedJson))
        {
            _logger.LogDebug("Cache hit for enrichment key '{Key}'", lookupKey);
            return cachedJson is not null ? JsonNode.Parse(cachedJson) : null;
        }

        var result = await _inner.FetchAsync(lookupKey, ct);

        var serialized = result?.ToJsonString();
        _cache.Set(cacheKey, serialized, _ttl);

        _logger.LogDebug("Cache miss for enrichment key '{Key}' — cached for {Ttl}", lookupKey, _ttl);
        return result;
    }
}
