using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Performance.Profiling;

/// <summary>
/// Extension methods for registering Performance.Profiling services.
/// </summary>
public static class ProfilingServiceExtensions
{
    /// <summary>
    /// Registers the continuous profiling, hotspot detection, GC monitoring,
    /// and benchmark registry services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration root for binding ProfilingOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPerformanceProfiling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ProfilingOptions>(configuration.GetSection("Profiling"));
        services.AddSingleton<IContinuousProfiler, ContinuousProfiler>();
        services.AddSingleton<IHotspotDetector, AllocationHotspotDetector>();
        services.AddSingleton<IGcMonitor, GcMonitor>();
        services.AddSingleton<IBenchmarkRegistry, InMemoryBenchmarkRegistry>();

        return services;
    }
}
