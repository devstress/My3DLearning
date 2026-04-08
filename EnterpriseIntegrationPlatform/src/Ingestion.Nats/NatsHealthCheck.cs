using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace EnterpriseIntegrationPlatform.Ingestion.Nats;

/// <summary>
/// Health check that verifies NATS JetStream API responsiveness.
/// Returns <see cref="HealthCheckResult.Healthy"/> if JetStream can list streams,
/// <see cref="HealthCheckResult.Unhealthy"/> otherwise.
/// </summary>
public sealed class NatsHealthCheck : IHealthCheck
{
    private readonly INatsConnection _connection;
    private readonly ILogger<NatsHealthCheck> _logger;

    public NatsHealthCheck(INatsConnection connection, ILogger<NatsHealthCheck> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var js = new NatsJSContext((NatsConnection)_connection);
            // List streams to verify JetStream API is responsive.
            // Enumerate at least one to prove the API call succeeds.
            var count = 0;
            await foreach (var _ in js.ListStreamsAsync(cancellationToken: cancellationToken))
            {
                count++;
                break; // Only need to confirm API works
            }
            return HealthCheckResult.Healthy($"NATS JetStream API responsive. Streams found: {(count > 0 ? "yes" : "none (empty)")}.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NATS JetStream health check failed");
            return HealthCheckResult.Unhealthy("NATS JetStream API unreachable.", ex);
        }
    }
}
