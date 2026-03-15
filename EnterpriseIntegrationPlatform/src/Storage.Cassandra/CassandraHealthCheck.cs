using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Storage.Cassandra;

/// <summary>
/// Health check that verifies Cassandra is reachable by executing a
/// lightweight system query. Registered automatically by
/// <see cref="CassandraServiceExtensions.AddCassandraStorage"/>.
/// </summary>
public sealed class CassandraHealthCheck : IHealthCheck
{
    private readonly ICassandraSessionFactory _sessionFactory;
    private readonly ILogger<CassandraHealthCheck> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="CassandraHealthCheck"/>.
    /// </summary>
    /// <param name="sessionFactory">The session factory to probe.</param>
    /// <param name="logger">Logger.</param>
    public CassandraHealthCheck(
        ICassandraSessionFactory sessionFactory,
        ILogger<CassandraHealthCheck> logger)
    {
        _sessionFactory = sessionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionFactory.GetSessionAsync(cancellationToken);
            var result = await session.ExecuteAsync(
                new global::Cassandra.SimpleStatement("SELECT release_version FROM system.local"));

            var row = result.FirstOrDefault();
            var version = row?.GetValue<string>("release_version") ?? "unknown";

            return HealthCheckResult.Healthy($"Cassandra is reachable (version {version}).");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cassandra health check failed");
            return HealthCheckResult.Unhealthy(
                "Cassandra is not reachable.",
                exception: ex);
        }
    }
}
