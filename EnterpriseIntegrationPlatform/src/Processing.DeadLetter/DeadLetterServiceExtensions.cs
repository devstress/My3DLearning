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
}
