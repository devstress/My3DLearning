using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.DeadLetter;

public static class DeadLetterServiceExtensions
{
    public static IServiceCollection AddDeadLetterPublisher<T>(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DeadLetterOptions>(configuration.GetSection("DeadLetter"));
        services.AddScoped<IDeadLetterPublisher<T>, DeadLetterPublisher<T>>();

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IMessageExpirationChecker{T}"/> that checks
    /// <see cref="Contracts.IntegrationEnvelope{T}.ExpiresAt"/> and routes expired
    /// messages to the Dead Letter Queue.
    /// </summary>
    public static IServiceCollection AddMessageExpirationChecker<T>(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IMessageExpirationChecker<T>, MessageExpirationChecker<T>>();
        return services;
    }
}
