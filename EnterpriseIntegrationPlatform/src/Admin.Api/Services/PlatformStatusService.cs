using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnterpriseIntegrationPlatform.Admin.Api.Services;

/// <summary>
/// Aggregates the operational status of all platform components by running
/// the registered ASP.NET Core health checks and returning a unified response.
/// </summary>
public sealed class PlatformStatusService(
    HealthCheckService healthCheckService,
    ILogger<PlatformStatusService> logger)
{
    /// <summary>
    /// Runs all registered health checks and returns an aggregated platform status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="PlatformStatusResult"/> describing the current state.</returns>
    public async Task<PlatformStatusResult> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        HealthReport report;
        try
        {
            report = await healthCheckService.CheckHealthAsync(null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check aggregation failed unexpectedly");
            return new PlatformStatusResult
            {
                Overall = nameof(HealthStatus.Unhealthy),
                Components = [],
                CheckedAt = DateTimeOffset.UtcNow,
                TotalDuration = TimeSpan.Zero,
            };
        }

        var components = report.Entries
            .Select(e => new ComponentStatus
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration,
            })
            .ToList();

        return new PlatformStatusResult
        {
            Overall = report.Status.ToString(),
            Components = components,
            CheckedAt = DateTimeOffset.UtcNow,
            TotalDuration = report.TotalDuration,
        };
    }
}

/// <summary>Aggregated platform status returned by <see cref="PlatformStatusService"/>.</summary>
public sealed record PlatformStatusResult
{
    /// <summary>Overall health: Healthy, Degraded, or Unhealthy.</summary>
    public required string Overall { get; init; }

    /// <summary>Individual component statuses.</summary>
    public required IReadOnlyList<ComponentStatus> Components { get; init; }

    /// <summary>UTC time at which the status was collected.</summary>
    public required DateTimeOffset CheckedAt { get; init; }

    /// <summary>Total time taken to run all health checks.</summary>
    public TimeSpan TotalDuration { get; init; }
}

/// <summary>Status of a single platform component.</summary>
public sealed record ComponentStatus
{
    /// <summary>Health check name (e.g. "cassandra", "self").</summary>
    public required string Name { get; init; }

    /// <summary>Health status string: Healthy, Degraded, or Unhealthy.</summary>
    public required string Status { get; init; }

    /// <summary>Optional description returned by the health check.</summary>
    public string? Description { get; init; }

    /// <summary>Time taken to run this individual check.</summary>
    public TimeSpan Duration { get; init; }
}
