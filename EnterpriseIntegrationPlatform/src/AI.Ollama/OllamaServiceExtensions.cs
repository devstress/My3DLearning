using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.AI.Ollama;

/// <summary>
/// Extension methods for registering the Ollama AI service
/// with the dependency injection container.
/// </summary>
public static class OllamaServiceExtensions
{
    /// <summary>Default base address for the Aspire-managed Ollama instance.</summary>
    public const string DefaultBaseAddress = "http://localhost:11434";

    /// <summary>
    /// Registers <see cref="IOllamaService"/> backed by
    /// <see cref="OllamaService"/> and adds an Ollama health check.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseAddress">
    /// Base URL of the Ollama API. Defaults to <c>http://localhost:11434</c>.
    /// </param>
    /// <param name="model">
    /// The Ollama model name. Defaults to <c>llama3.2</c>.
    /// CI may override to a smaller model (e.g. <c>qwen2.5:0.5b</c>).
    /// </param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddOllamaService(
        this IServiceCollection services,
        string baseAddress = DefaultBaseAddress,
        string model = "llama3.2")
    {
        services.Configure<OllamaSettings>(opts => opts.Model = model);

        services.AddHttpClient<IOllamaService, OllamaService>(client =>
        {
            client.BaseAddress = new Uri(baseAddress);
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddHealthChecks()
            .AddCheck<OllamaHealthCheck>("ollama", tags: ["ready"]);

        return services;
    }
}
