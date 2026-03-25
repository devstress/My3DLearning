using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.Retry;

public static class RetryServiceExtensions
{
    public static IServiceCollection AddRetryPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RetryOptions>(configuration.GetSection("Retry"));
        services.AddSingleton<IRetryPolicy, ExponentialBackoffRetryPolicy>();

        return services;
    }
}
