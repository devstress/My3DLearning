using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Pulsar;

/// <summary>
/// Health check that verifies Apache Pulsar client connectivity.
/// Returns <see cref="HealthCheckResult.Healthy"/> if the Pulsar client is operational,
/// <see cref="HealthCheckResult.Unhealthy"/> otherwise.
/// </summary>
public sealed class PulsarHealthCheck : IHealthCheck
{
    private readonly IPulsarClient _client;
    private readonly ILogger<PulsarHealthCheck> _logger;

    /// <summary>Initialises a new <see cref="PulsarHealthCheck"/>.</summary>
    public PulsarHealthCheck(IPulsarClient client, ILogger<PulsarHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the client instance exists and can create a producer builder
            // (lightweight call, no I/O). NewProducer() is an extension method.
            var builder = _client.NewProducer();
            if (builder is null)
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy("Pulsar client returned null producer builder."));
            }

            return Task.FromResult(
                HealthCheckResult.Healthy("Pulsar client is operational."));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pulsar health check failed");
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Pulsar client unreachable.", ex));
        }
    }
}
