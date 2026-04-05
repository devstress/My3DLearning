using System.Text.Json.Nodes;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Abstraction for fetching enrichment data from an external source.
/// Implementations include HTTP endpoints, databases, and caches.
/// </summary>
public interface IEnrichmentSource
{
    /// <summary>
    /// Fetches enrichment data for the given lookup key.
    /// </summary>
    /// <param name="lookupKey">The key extracted from the source payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="JsonNode"/> containing the enrichment data, or <see langword="null"/> if not found.</returns>
    Task<JsonNode?> FetchAsync(string lookupKey, CancellationToken ct = default);
}
