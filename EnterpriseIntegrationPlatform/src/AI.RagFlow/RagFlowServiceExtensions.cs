using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.AI.RagFlow;

/// <summary>
/// Extension methods for registering the RagFlow RAG service
/// with the dependency injection container.
/// </summary>
public static class RagFlowServiceExtensions
{
    /// <summary>Default base address for the Aspire-managed RagFlow instance.</summary>
    public const string DefaultBaseAddress = "http://localhost:15380";

    /// <summary>
    /// Registers <see cref="IRagFlowService"/> backed by <see cref="RagFlowService"/>
    /// and adds a RagFlow health check. Configuration is read from the
    /// <c>RagFlow</c> section in <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration (for RagFlow section binding).</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddRagFlowService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new RagFlowOptions();
        configuration.GetSection(RagFlowOptions.SectionName).Bind(options);

        services.AddSingleton(options);

        services.AddHttpClient<IRagFlowService, RagFlowService>((sp, client) =>
        {
            client.BaseAddress = new Uri(options.BaseAddress);
            client.Timeout = TimeSpan.FromSeconds(60);

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
            }
        });

        services.AddHealthChecks()
            .AddCheck<RagFlowHealthCheck>("ragflow", tags: ["ready"]);

        return services;
    }
}
