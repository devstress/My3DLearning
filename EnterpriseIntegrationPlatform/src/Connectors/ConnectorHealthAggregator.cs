using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Connectors;

/// <summary>
/// Aggregates health status from all registered connectors in the <see cref="IConnectorRegistry"/>.
/// Returns <see cref="HealthStatus.Healthy"/> when all connectors pass their health probe,
/// <see cref="HealthStatus.Degraded"/> when at least one fails, and
/// <see cref="HealthStatus.Unhealthy"/> when all connectors fail or none are registered.
/// </summary>
public sealed class ConnectorHealthAggregator : IHealthCheck
{
    private readonly IConnectorRegistry _registry;
    private readonly ILogger<ConnectorHealthAggregator> _logger;

    /// <summary>Initialises a new instance of <see cref="ConnectorHealthAggregator"/>.</summary>
    /// <param name="registry">The connector registry to probe.</param>
    /// <param name="logger">Logger instance.</param>
    public ConnectorHealthAggregator(
        IConnectorRegistry registry,
        ILogger<ConnectorHealthAggregator> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectors = _registry.GetAll();

        if (connectors.Count == 0)
        {
            _logger.LogWarning("No connectors registered for health aggregation");
            return HealthCheckResult.Unhealthy("No connectors are registered.");
        }

        var results = new Dictionary<string, object>();
        var healthyCount = 0;

        foreach (var connector in connectors)
        {
            var healthy = await ProbeConnectorAsync(connector, cancellationToken);
            results[connector.Name] = healthy ? "Healthy" : "Unhealthy";

            if (healthy)
            {
                healthyCount++;
            }
        }

        var data = results.AsReadOnly();

        if (healthyCount == connectors.Count)
        {
            _logger.LogDebug("All {Count} connectors are healthy", connectors.Count);
            return HealthCheckResult.Healthy(
                $"All {connectors.Count} connectors are healthy.", data);
        }

        if (healthyCount == 0)
        {
            _logger.LogWarning("All {Count} connectors are unhealthy", connectors.Count);
            return HealthCheckResult.Unhealthy(
                $"All {connectors.Count} connectors are unhealthy.", data: data);
        }

        _logger.LogWarning(
            "{HealthyCount} of {TotalCount} connectors are healthy",
            healthyCount, connectors.Count);

        return HealthCheckResult.Degraded(
            $"{healthyCount} of {connectors.Count} connectors are healthy.", data: data);
    }

    private async Task<bool> ProbeConnectorAsync(
        IConnector connector,
        CancellationToken cancellationToken)
    {
        try
        {
            return await connector.TestConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Health probe for connector '{ConnectorName}' threw an exception",
                connector.Name);
            return false;
        }
    }
}
