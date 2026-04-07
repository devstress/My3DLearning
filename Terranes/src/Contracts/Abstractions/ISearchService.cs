using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Cross-entity search service for discovering homes, villages, listings, and partners.
/// </summary>
public interface ISearchService
{
    /// <summary>Searches across all entity types using a text query.</summary>
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int maxResults = 20, CancellationToken cancellationToken = default);

    /// <summary>Searches within a specific entity type.</summary>
    Task<IReadOnlyList<SearchResult>> SearchByTypeAsync(string entityType, string query, int maxResults = 20, CancellationToken cancellationToken = default);

    /// <summary>Indexes an entity for future search.</summary>
    Task IndexEntityAsync(string entityType, Guid entityId, string title, string summary, CancellationToken cancellationToken = default);

    /// <summary>Removes an entity from the index.</summary>
    Task RemoveEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>Gets total indexed entity count.</summary>
    Task<int> GetIndexedCountAsync(CancellationToken cancellationToken = default);
}
