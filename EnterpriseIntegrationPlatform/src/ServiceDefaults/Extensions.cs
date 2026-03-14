using Microsoft.Extensions.Hosting;

namespace EnterpriseIntegrationPlatform.ServiceDefaults;

/// <summary>
/// Shared service configuration extensions for the Enterprise Integration Platform.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds default platform services including health checks and OpenTelemetry.
    /// </summary>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        // Will be configured in subsequent chunks
        return builder;
    }
}
