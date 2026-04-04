namespace EnterpriseIntegrationPlatform.AI.RagKnowledge;

/// <summary>
/// Represents a single knowledge document parsed from the XML RAG knowledge base.
/// Each document corresponds to one Enterprise Integration Pattern or platform concept.
/// </summary>
/// <param name="Id">Unique identifier for the document (e.g. "content-based-router").</param>
/// <param name="Title">Human-readable title (e.g. "Content-Based Router").</param>
/// <param name="Pattern">The EIP pattern name this document describes.</param>
/// <param name="Category">The category grouping (e.g. "Message Routing", "Messaging Endpoints").</param>
/// <param name="Summary">A concise description of the pattern or concept.</param>
/// <param name="Implementation">How the pattern is implemented in the platform.</param>
/// <param name="Components">Comma-separated list of platform components involved.</param>
/// <param name="Tags">Search tags for keyword matching.</param>
public sealed record RagDocument(
    string Id,
    string Title,
    string Pattern,
    string Category,
    string Summary,
    string Implementation,
    string Components,
    IReadOnlyList<string> Tags);
