namespace EnterpriseIntegrationPlatform.AI.RagFlow;

/// <summary>
/// Configuration options for the RagFlow RAG service.
/// Bind from the <c>RagFlow</c> configuration section.
/// </summary>
public sealed class RagFlowOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "RagFlow";

    /// <summary>
    /// Base URL of the RagFlow API (e.g. <c>http://localhost:15380</c>).
    /// </summary>
    public string BaseAddress { get; set; } = "http://localhost:15380";

    /// <summary>
    /// RagFlow API key for authentication. Required for all API calls.
    /// Store in user secrets or environment variables — never in source code.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The RagFlow chat assistant ID to use for integration generation.
    /// Create an assistant in the RagFlow UI, link it to the platform's
    /// knowledge base datasets, then set this value.
    /// </summary>
    public string? AssistantId { get; set; }
}
