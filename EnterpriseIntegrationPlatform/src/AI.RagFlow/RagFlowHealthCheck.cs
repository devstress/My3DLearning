using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnterpriseIntegrationPlatform.AI.RagFlow;

/// <summary>
/// Health check that verifies the RagFlow RAG service is reachable.
/// Registered automatically by <see cref="RagFlowServiceExtensions.AddRagFlowService"/>.
/// </summary>
public sealed class RagFlowHealthCheck : IHealthCheck
{
    private readonly IRagFlowService _ragFlow;

    /// <summary>
    /// Initialises a new instance of <see cref="RagFlowHealthCheck"/>.
    /// </summary>
    /// <param name="ragFlow">The RagFlow service to probe.</param>
    public RagFlowHealthCheck(IRagFlowService ragFlow)
    {
        _ragFlow = ragFlow;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var healthy = await _ragFlow.IsHealthyAsync(cancellationToken);
        return healthy
            ? HealthCheckResult.Healthy("RagFlow is reachable.")
            : HealthCheckResult.Degraded("RagFlow is not reachable. RAG knowledge retrieval is unavailable.");
    }
}
