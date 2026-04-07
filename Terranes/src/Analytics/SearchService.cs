using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Analytics;

/// <summary>
/// In-memory implementation of <see cref="ISearchService"/>.
/// Provides cross-entity full-text search using simple substring matching.
/// </summary>
public sealed class SearchService : ISearchService
{
    private readonly ConcurrentDictionary<(string EntityType, Guid EntityId), (string Title, string Summary)> _index = new();
    private readonly ILogger<SearchService> _logger;

    public SearchService(ILogger<SearchService> logger) => _logger = logger;

    public Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query is required.", nameof(query));

        IReadOnlyList<SearchResult> results = _index
            .Select(kvp => new { kvp.Key.EntityType, kvp.Key.EntityId, kvp.Value.Title, kvp.Value.Summary, Score = CalculateScore(query, kvp.Value.Title, kvp.Value.Summary) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => new SearchResult(x.EntityType, x.EntityId, x.Title, x.Summary, x.Score))
            .ToList();

        _logger.LogInformation("Search returned {Count} results", results.Count);
        return Task.FromResult(results);
    }

    public Task<IReadOnlyList<SearchResult>> SearchByTypeAsync(string entityType, string query, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type is required.", nameof(entityType));

        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query is required.", nameof(query));

        IReadOnlyList<SearchResult> results = _index
            .Where(kvp => string.Equals(kvp.Key.EntityType, entityType, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new { kvp.Key.EntityType, kvp.Key.EntityId, kvp.Value.Title, kvp.Value.Summary, Score = CalculateScore(query, kvp.Value.Title, kvp.Value.Summary) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => new SearchResult(x.EntityType, x.EntityId, x.Title, x.Summary, x.Score))
            .ToList();

        return Task.FromResult(results);
    }

    public Task IndexEntityAsync(string entityType, Guid entityId, string title, string summary, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type is required.", nameof(entityType));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        _index[(entityType, entityId)] = (title, summary ?? string.Empty);
        return Task.CompletedTask;
    }

    public Task RemoveEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        _index.TryRemove((entityType, entityId), out _);
        return Task.CompletedTask;
    }

    public Task<int> GetIndexedCountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_index.Count);

    private static double CalculateScore(string query, string title, string summary)
    {
        var q = query.ToUpperInvariant();
        var titleUpper = title.ToUpperInvariant();
        var summaryUpper = summary.ToUpperInvariant();

        double score = 0;

        // Exact title match is highest
        if (titleUpper.Contains(q))
            score += 10.0;

        // Summary match
        if (summaryUpper.Contains(q))
            score += 5.0;

        // Individual word matches
        var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (titleUpper.Contains(word)) score += 2.0;
            if (summaryUpper.Contains(word)) score += 1.0;
        }

        return score;
    }
}
