using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.AI.RagKnowledge;

/// <summary>
/// Builds an in-memory search index from parsed <see cref="RagDocument"/> entries.
/// The index maps normalized keywords to documents for fast retrieval.
/// </summary>
public sealed class RagKnowledgeIndex
{
    private readonly ILogger<RagKnowledgeIndex> _logger;
    private readonly Dictionary<string, List<RagDocument>> _tagIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RagDocument> _idIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RagDocument> _allDocuments = [];

    public RagKnowledgeIndex(ILogger<RagKnowledgeIndex> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Gets the total number of indexed documents.</summary>
    public int DocumentCount => _allDocuments.Count;

    /// <summary>Gets all indexed documents.</summary>
    public IReadOnlyList<RagDocument> AllDocuments => _allDocuments;

    /// <summary>
    /// Adds a collection of documents to the index.
    /// </summary>
    public void AddDocuments(IEnumerable<RagDocument> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        foreach (var doc in documents)
        {
            _allDocuments.Add(doc);
            _idIndex[doc.Id] = doc;

            // Index by tags
            foreach (var tag in doc.Tags)
            {
                if (!_tagIndex.TryGetValue(tag, out var list))
                {
                    list = [];
                    _tagIndex[tag] = list;
                }
                list.Add(doc);
            }

            // Also index by title words and category words
            IndexWords(doc, doc.Title);
            IndexWords(doc, doc.Category);
            IndexWords(doc, doc.Pattern);
        }

        _logger.LogInformation("Index contains {Count} documents with {Tags} tag entries",
            _allDocuments.Count, _tagIndex.Count);
    }

    /// <summary>
    /// Retrieves a document by its unique ID.
    /// </summary>
    public RagDocument? GetById(string id)
    {
        return _idIndex.TryGetValue(id, out var doc) ? doc : null;
    }

    /// <summary>
    /// Gets all unique tag keys in the index.
    /// </summary>
    public IReadOnlyList<string> GetAllTags()
    {
        return [.. _tagIndex.Keys.Order()];
    }

    /// <summary>
    /// Retrieves documents matching a specific tag.
    /// </summary>
    public IReadOnlyList<RagDocument> GetByTag(string tag)
    {
        return _tagIndex.TryGetValue(tag, out var list) ? list : [];
    }

    /// <summary>
    /// Gets all documents in a specific category.
    /// </summary>
    public IReadOnlyList<RagDocument> GetByCategory(string category)
    {
        return _allDocuments
            .Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void IndexWords(RagDocument doc, string text)
    {
        var words = text.Split([' ', '-', '_', '.', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var word in words)
        {
            if (word.Length < 2) continue;
            if (!_tagIndex.TryGetValue(word, out var list))
            {
                list = [];
                _tagIndex[word] = list;
            }
            if (!list.Contains(doc))
                list.Add(doc);
        }
    }
}
