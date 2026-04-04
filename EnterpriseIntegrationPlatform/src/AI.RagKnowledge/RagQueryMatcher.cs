using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.AI.RagKnowledge;

/// <summary>
/// Searches the <see cref="RagKnowledgeIndex"/> using natural-language queries.
/// Splits the query into keywords, scores documents by keyword overlap, and returns
/// ranked results. This provides local, offline query matching without requiring
/// the full RagFlow service to be running.
/// </summary>
public sealed class RagQueryMatcher
{
    private readonly RagKnowledgeIndex _index;
    private readonly ILogger<RagQueryMatcher> _logger;

    /// <summary>Words that add noise and should be excluded from keyword matching.</summary>
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "is", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "do", "does", "did", "will", "would", "shall",
        "should", "may", "might", "must", "can", "could", "to", "of", "in",
        "for", "on", "with", "at", "by", "from", "as", "into", "through",
        "during", "before", "after", "and", "but", "or", "nor", "not", "no",
        "so", "if", "then", "than", "that", "this", "these", "those", "it",
        "its", "what", "which", "who", "whom", "how", "when", "where", "why",
        "all", "each", "every", "both", "few", "more", "most", "other", "some",
        "such", "only", "own", "same", "about", "up", "out", "just", "also",
    };

    public RagQueryMatcher(RagKnowledgeIndex index, ILogger<RagQueryMatcher> logger)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches the knowledge base for documents matching the query.
    /// </summary>
    /// <param name="query">Natural language query (e.g. "How does content-based routing work?").</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>Ranked list of matching documents with relevance scores.</returns>
    public IReadOnlyList<RagQueryResult> Search(string query, int maxResults = 5)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var keywords = ExtractKeywords(query);
        if (keywords.Count == 0)
        {
            _logger.LogDebug("Query '{Query}' produced no searchable keywords", query);
            return [];
        }

        _logger.LogDebug("Searching for keywords: {Keywords}", string.Join(", ", keywords));

        // Score each document by keyword overlap
        var scores = new Dictionary<string, (RagDocument Doc, double Score)>();

        foreach (var keyword in keywords)
        {
            var matches = _index.GetByTag(keyword);
            foreach (var doc in matches)
            {
                if (scores.TryGetValue(doc.Id, out var existing))
                {
                    scores[doc.Id] = (doc, existing.Score + 1.0);
                }
                else
                {
                    scores[doc.Id] = (doc, 1.0);
                }
            }

            // Bonus for exact title match
            foreach (var doc in _index.AllDocuments)
            {
                if (doc.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    doc.Pattern.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    if (scores.TryGetValue(doc.Id, out var existing))
                    {
                        scores[doc.Id] = (doc, existing.Score + 0.5);
                    }
                    else
                    {
                        scores[doc.Id] = (doc, 0.5);
                    }
                }
            }
        }

        // Normalize scores (0.0–1.0) and sort descending
        if (scores.Count == 0)
        {
            _logger.LogDebug("No results found for query '{Query}'", query);
            return [];
        }

        var maxScore = scores.Values.Max(s => s.Score);
        var results = scores.Values
            .OrderByDescending(s => s.Score)
            .Take(maxResults)
            .Select(s => new RagQueryResult(s.Doc, maxScore > 0 ? s.Score / maxScore : 0.0))
            .ToList();

        _logger.LogDebug("Found {Count} results for query '{Query}'", results.Count, query);
        return results;
    }

    /// <summary>
    /// Extracts meaningful keywords from a natural-language query.
    /// </summary>
    internal static IReadOnlyList<string> ExtractKeywords(string query)
    {
        var words = query.Split([' ', '-', '_', '.', ',', '?', '!', '\'', '"', '(', ')'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return words
            .Where(w => w.Length >= 2 && !StopWords.Contains(w))
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .ToList();
    }
}

/// <summary>
/// A search result from the RAG knowledge base query matcher.
/// </summary>
/// <param name="Document">The matching knowledge document.</param>
/// <param name="Score">Relevance score normalized to 0.0–1.0.</param>
public sealed record RagQueryResult(RagDocument Document, double Score);
