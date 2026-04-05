using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Connector.Sftp;

/// <summary>
/// Service-collection extensions for registering the SFTP connector.
/// </summary>
public static class SftpConnectorServiceExtensions
{
    /// <summary>
    /// Registers <see cref="SftpConnector"/> as <see cref="ISftpConnector"/> (scoped),
    /// <see cref="SshNetSftpClient"/> as <see cref="ISftpClient"/> (scoped),
    /// <see cref="SftpConnectionPool"/> as <see cref="ISftpConnectionPool"/> (singleton),
    /// and binds options from the <c>SftpConnector</c> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSftpConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SftpConnectorOptions>(configuration.GetSection("SftpConnector"));
        services.AddScoped<ISftpClient, SshNetSftpClient>();

        // Connection pool is a singleton so connections survive across scoped lifetimes.
        services.AddSingleton<ISftpConnectionPool>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<SftpConnectorOptions>>();
            var logger = sp.GetRequiredService<ILogger<SftpConnectionPool>>();
            return new SftpConnectionPool(
                () => new SshNetSftpClient(opts),
                opts,
                logger);
        });

        services.AddScoped<ISftpConnector, SftpConnector>();

        return services;
    }
}
