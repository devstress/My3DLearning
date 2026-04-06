// ============================================================================
// MockEnrichmentSource – Configurable enrichment lookup for testing
// ============================================================================

using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using EnterpriseIntegrationPlatform.Processing.Transform;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="IEnrichmentSource"/> backed by
/// a configurable lookup dictionary.
/// </summary>
public sealed class MockEnrichmentSource : IEnrichmentSource
{
    private readonly Dictionary<string, JsonNode?> _data = new();
    private readonly ConcurrentQueue<string> _lookups = new();
    private JsonNode? _fallback;

    /// <summary>All lookup keys requested.</summary>
    public IReadOnlyList<string> Lookups => _lookups.ToArray();

    /// <summary>Number of lookups performed.</summary>
    public int LookupCount => _lookups.Count;

    /// <summary>Adds a lookup entry.</summary>
    public MockEnrichmentSource WithData(string key, string json)
    {
        _data[key] = JsonNode.Parse(json);
        return this;
    }

    /// <summary>Adds a null lookup entry (key exists but returns null).</summary>
    public MockEnrichmentSource WithNull(string key)
    {
        _data[key] = null;
        return this;
    }

    /// <summary>Sets a fallback value for unknown keys.</summary>
    public MockEnrichmentSource WithFallback(string? json)
    {
        _fallback = json is not null ? JsonNode.Parse(json) : null;
        return this;
    }

    /// <summary>Configures the source to return null for all unknown keys.</summary>
    public MockEnrichmentSource ReturnsNullForUnknown()
    {
        _fallback = null;
        return this;
    }

    public Task<JsonNode?> FetchAsync(string lookupKey, CancellationToken ct = default)
    {
        _lookups.Enqueue(lookupKey);

        if (_data.TryGetValue(lookupKey, out var node))
            return Task.FromResult(node);

        return Task.FromResult(_fallback);
    }

    public void Reset()
    {
        while (_lookups.TryDequeue(out _)) { }
    }
}
