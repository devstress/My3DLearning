using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.AI.RagKnowledge;

/// <summary>
/// Extension methods to register RAG knowledge services in the DI container.
/// </summary>
public static class RagKnowledgeServiceExtensions
{
    /// <summary>
    /// Registers <see cref="RagDocumentParser"/>, <see cref="RagKnowledgeIndex"/>,
    /// and <see cref="RagQueryMatcher"/> as singleton services and optionally
    /// pre-loads knowledge documents from a directory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="knowledgeDirectory">
    /// Optional path to the docs/rag/ directory. When provided, XML documents
    /// are parsed and indexed at first resolution of <see cref="RagKnowledgeIndex"/>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRagKnowledge(this IServiceCollection services, string? knowledgeDirectory = null)
    {
        services.AddSingleton<RagDocumentParser>();
        services.AddSingleton(sp =>
        {
            var index = new RagKnowledgeIndex(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RagKnowledgeIndex>>());
            if (!string.IsNullOrWhiteSpace(knowledgeDirectory))
            {
                var parser = sp.GetRequiredService<RagDocumentParser>();
                var docs = parser.ParseDirectory(knowledgeDirectory);
                index.AddDocuments(docs);
            }
            return index;
        });
        services.AddSingleton<RagQueryMatcher>();

        return services;
    }
}
