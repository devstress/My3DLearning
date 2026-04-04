using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    /// are parsed and indexed during registration.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRagKnowledge(this IServiceCollection services, string? knowledgeDirectory = null)
    {
        services.AddSingleton<RagDocumentParser>();
        services.AddSingleton<RagKnowledgeIndex>();
        services.AddSingleton<RagQueryMatcher>();

        if (!string.IsNullOrWhiteSpace(knowledgeDirectory))
        {
            services.AddSingleton(sp =>
            {
                var parser = sp.GetRequiredService<RagDocumentParser>();
                var index = sp.GetRequiredService<RagKnowledgeIndex>();
                var docs = parser.ParseDirectory(knowledgeDirectory);
                index.AddDocuments(docs);
                return index;
            });
        }

        return services;
    }
}
