using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.Throttle;

/// <summary>
/// Extension methods for registering the message processing throttle.
/// </summary>
public static class ThrottleServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IMessageThrottle"/> (global) and
    /// <see cref="IThrottleRegistry"/> (partitioned per tenant/queue/endpoint).
    /// Configuration is bound from the <c>Throttle</c> section.
    /// </summary>
    public static IServiceCollection AddMessageThrottle(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ThrottleOptions>(
            configuration.GetSection(ThrottleOptions.SectionName));

        // Global single-partition throttle (for simple use cases).
        services.AddSingleton<IMessageThrottle, TokenBucketThrottle>();

        // Partitioned registry with per-tenant / per-queue / per-endpoint
        // throttle policies — controllable from Admin API at runtime.
        services.AddSingleton<IThrottleRegistry, ThrottleRegistry>();

        return services;
    }
}
