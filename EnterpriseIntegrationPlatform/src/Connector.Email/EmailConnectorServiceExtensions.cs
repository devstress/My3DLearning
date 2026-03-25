using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Connector.Email;

/// <summary>
/// Service-collection extensions for registering the Email connector.
/// </summary>
public static class EmailConnectorServiceExtensions
{
    /// <summary>
    /// Registers <see cref="EmailConnector"/> as <see cref="IEmailConnector"/> (scoped),
    /// <see cref="MailKitSmtpClientWrapper"/> as <see cref="ISmtpClientWrapper"/> (scoped),
    /// and binds options from the <c>EmailConnector</c> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddEmailConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<EmailConnectorOptions>(configuration.GetSection("EmailConnector"));
        services.AddScoped<ISmtpClientWrapper, MailKitSmtpClientWrapper>();
        services.AddScoped<IEmailConnector, EmailConnector>();

        return services;
    }
}
