using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Connector.FileSystem;

/// <summary>
/// Service-collection extensions for registering the File connector.
/// </summary>
public static class FileConnectorServiceExtensions
{
    /// <summary>
    /// Registers <see cref="FileConnector"/> as <see cref="IFileConnector"/> (scoped),
    /// <see cref="PhysicalFileSystem"/> as <see cref="IFileSystem"/> (singleton), and binds
    /// options from the <c>FileConnector</c> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddFileConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<FileConnectorOptions>(configuration.GetSection("FileConnector"));
        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        services.AddScoped<IFileConnector, FileConnector>();

        return services;
    }
}
