using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Connector.Http;

/// <summary>
/// Service-collection extensions for registering the HTTP connector.
/// </summary>
public static class HttpConnectorServiceExtensions
{
    /// <summary>
    /// Registers <see cref="HttpConnector"/> as <see cref="IHttpConnector"/>,
    /// <see cref="InMemoryTokenCache"/> as <see cref="ITokenCache"/>, and a named
    /// <see cref="System.Net.Http.HttpClient"/> with base address and resilience pipeline
    /// derived from the <c>HttpConnector</c> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddHttpConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<HttpConnectorOptions>(configuration.GetSection("HttpConnector"));

        services.AddSingleton<ITokenCache, InMemoryTokenCache>();

        services
            .AddHttpClient("HttpConnector", (sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<HttpConnectorOptions>>().Value;
                if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
                    client.BaseAddress = new Uri(opts.BaseUrl);

                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds > 0 ? opts.TimeoutSeconds : 30);

                foreach (var (key, value) in opts.DefaultHeaders)
                    client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
            })
            .AddStandardResilienceHandler(options =>
            {
                // Retry and timeout configuration is applied via the resilience pipeline.
                // Values are intentionally left at defaults here; override via
                // IOptions<HttpStandardResilienceOptions> in host configuration if needed.
            });

        services.AddScoped<IHttpConnector, HttpConnector>();

        return services;
    }
}
