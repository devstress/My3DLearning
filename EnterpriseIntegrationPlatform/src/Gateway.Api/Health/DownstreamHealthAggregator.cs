using EnterpriseIntegrationPlatform.Gateway.Api.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Gateway.Api.Health;

/// <summary>
/// Aggregates health status from all downstream services (Admin.Api, OpenClaw.Web).
/// Returns <see cref="HealthStatus.Healthy"/> when all downstream services are healthy,
/// <see cref="HealthStatus.Degraded"/> when at least one is unreachable, and
/// <see cref="HealthStatus.Unhealthy"/> when all are unreachable.
/// </summary>
public sealed class DownstreamHealthAggregator : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly GatewayOptions _options;
    private readonly ILogger<DownstreamHealthAggregator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DownstreamHealthAggregator"/>.
    /// </summary>
    /// <param name="httpClient">HTTP client for downstream health requests.</param>
    /// <param name="options">Gateway configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public DownstreamHealthAggregator(
        HttpClient httpClient,
        IOptions<GatewayOptions> options,
        ILogger<DownstreamHealthAggregator> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var services = new Dictionary<string, string>
        {
            ["AdminApi"] = $"{_options.AdminApiBaseUrl.TrimEnd('/')}/health",
            ["OpenClaw"] = $"{_options.OpenClawBaseUrl.TrimEnd('/')}/health",
        };

        var results = new Dictionary<string, object>();
        var healthyCount = 0;

        foreach (var (name, url) in services)
        {
            var healthy = await CheckServiceAsync(name, url, cancellationToken);
            results[name] = healthy ? "Healthy" : "Unhealthy";
            if (healthy)
            {
                healthyCount++;
            }
        }

        var data = results.AsReadOnly();

        if (healthyCount == services.Count)
        {
            return HealthCheckResult.Healthy("All downstream services are healthy.", data);
        }

        if (healthyCount == 0)
        {
            return HealthCheckResult.Unhealthy("All downstream services are unhealthy.", data: data);
        }

        return HealthCheckResult.Degraded("One or more downstream services are unhealthy.", data: data);
    }

    private async Task<bool> CheckServiceAsync(
        string name,
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check for {ServiceName} at {Url} failed", name, url);
            return false;
        }
    }
}
