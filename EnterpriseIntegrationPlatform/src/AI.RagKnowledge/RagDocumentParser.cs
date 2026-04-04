using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.AI.RagKnowledge;

/// <summary>
/// Parses XML-based RAG knowledge documents from the docs/rag/ directory.
/// Each XML file follows the <c>urn:eip:rag:v1</c> schema and contains
/// one or more <c>&lt;document&gt;</c> elements representing indexed knowledge articles.
/// </summary>
public sealed class RagDocumentParser
{
    private static readonly XNamespace Ns = "urn:eip:rag:v1";
    private readonly ILogger<RagDocumentParser> _logger;

    public RagDocumentParser(ILogger<RagDocumentParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parses a single XML knowledge file and returns the contained documents.
    /// </summary>
    /// <param name="xmlContent">The raw XML content to parse.</param>
    /// <returns>A list of parsed <see cref="RagDocument"/> entries.</returns>
    public IReadOnlyList<RagDocument> Parse(string xmlContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlContent);

        try
        {
            var xDoc = XDocument.Parse(xmlContent);
            var root = xDoc.Root;
            if (root is null)
            {
                _logger.LogWarning("XML document has no root element");
                return [];
            }

            var category = root.Attribute("category")?.Value ?? "Unknown";
            var documents = new List<RagDocument>();

            foreach (var elem in root.Elements(Ns + "document"))
            {
                var id = elem.Attribute("id")?.Value ?? string.Empty;
                var title = elem.Attribute("title")?.Value ?? string.Empty;
                var pattern = elem.Attribute("pattern")?.Value ?? string.Empty;
                var summary = elem.Element(Ns + "summary")?.Value.Trim() ?? string.Empty;
                var implementation = elem.Element(Ns + "implementation")?.Value.Trim() ?? string.Empty;
                var components = elem.Element(Ns + "components")?.Value.Trim() ?? string.Empty;
                var tagsRaw = elem.Element(Ns + "tags")?.Value.Trim() ?? string.Empty;
                var tags = tagsRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                documents.Add(new RagDocument(id, title, pattern, category, summary, implementation, components, tags));
            }

            _logger.LogDebug("Parsed {Count} documents from category '{Category}'", documents.Count, category);
            return documents;
        }
        catch (System.Xml.XmlException ex)
        {
            _logger.LogError(ex, "Failed to parse XML knowledge document");
            return [];
        }
    }

    /// <summary>
    /// Parses all XML files from a directory and returns all documents.
    /// </summary>
    /// <param name="directoryPath">Path to the directory containing XML knowledge files.</param>
    /// <returns>All parsed documents from all XML files.</returns>
    public IReadOnlyList<RagDocument> ParseDirectory(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("RAG knowledge directory not found: {Path}", directoryPath);
            return [];
        }

        var allDocuments = new List<RagDocument>();
        foreach (var file in Directory.GetFiles(directoryPath, "*.xml"))
        {
            var content = File.ReadAllText(file);
            var docs = Parse(content);
            allDocuments.AddRange(docs);
            _logger.LogDebug("Loaded {Count} documents from {File}", docs.Count, Path.GetFileName(file));
        }

        _logger.LogInformation("Loaded {Total} RAG knowledge documents from {Dir}", allDocuments.Count, directoryPath);
        return allDocuments;
    }
}
