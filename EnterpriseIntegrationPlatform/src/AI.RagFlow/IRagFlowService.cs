namespace EnterpriseIntegrationPlatform.AI.RagFlow;

/// <summary>
/// Interface for Retrieval-Augmented Generation (RAG) operations using
/// a self-hosted RagFlow instance. RagFlow chunks and indexes the platform's
/// source code, rules, and documentation so that AI code generation requests
/// receive relevant context automatically.
/// </summary>
public interface IRagFlowService
{
    /// <summary>
    /// Sends a natural-language query to RagFlow and returns contextually
    /// relevant chunks retrieved from the indexed knowledge base.
    /// </summary>
    /// <param name="query">The user query (e.g. "Generate an HTTP connector for the Acme API").</param>
    /// <param name="datasetIds">Optional dataset IDs to scope the search. When empty, searches all datasets.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Retrieved context as a single string, or empty if no results.</returns>
    Task<string> RetrieveAsync(string query, IReadOnlyList<string>? datasetIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a chat completion request to a RagFlow assistant, which performs
    /// RAG retrieval and LLM generation in a single call. The assistant is
    /// pre-configured with the platform's knowledge base.
    /// </summary>
    /// <param name="question">The user question or generation request.</param>
    /// <param name="conversationId">
    /// Optional conversation ID for multi-turn chat. Pass <c>null</c> to start
    /// a new conversation. RagFlow returns a conversation ID in the response.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI-generated response with embedded context from the knowledge base.</returns>
    Task<RagFlowChatResponse> ChatAsync(string question, string? conversationId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all datasets (knowledge bases) available in RagFlow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of dataset summaries.</returns>
    Task<IReadOnlyList<RagFlowDataset>> ListDatasetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> when the RagFlow service is reachable and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from a RagFlow chat completion request.
/// </summary>
/// <param name="Answer">The AI-generated answer.</param>
/// <param name="ConversationId">The conversation ID for multi-turn follow-up.</param>
/// <param name="References">Source references used to generate the answer.</param>
public sealed record RagFlowChatResponse(
    string Answer,
    string? ConversationId,
    IReadOnlyList<RagFlowReference> References);

/// <summary>
/// A source reference from a RAG retrieval result.
/// </summary>
/// <param name="Content">The text content of the chunk.</param>
/// <param name="DocumentName">The name of the source document.</param>
/// <param name="Score">Relevance score (0.0–1.0).</param>
public sealed record RagFlowReference(
    string Content,
    string? DocumentName,
    double Score);

/// <summary>
/// Summary of a RagFlow dataset (knowledge base).
/// </summary>
/// <param name="Id">The dataset ID.</param>
/// <param name="Name">The dataset name.</param>
/// <param name="DocumentCount">Number of documents in the dataset.</param>
public sealed record RagFlowDataset(
    string Id,
    string Name,
    int DocumentCount);
