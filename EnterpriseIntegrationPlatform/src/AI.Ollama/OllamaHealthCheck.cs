using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnterpriseIntegrationPlatform.AI.Ollama;

/// <summary>
/// Health check that verifies the Ollama service is reachable.
/// Registered automatically by <see cref="OllamaServiceExtensions.AddOllamaService"/>.
/// </summary>
public sealed class OllamaHealthCheck : IHealthCheck
{
    private readonly IOllamaService _ollama;

    /// <summary>
    /// Initialises a new instance of <see cref="OllamaHealthCheck"/>.
    /// </summary>
    /// <param name="ollama">The Ollama service to probe.</param>
    public OllamaHealthCheck(IOllamaService ollama)
    {
        _ollama = ollama;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var healthy = await _ollama.IsHealthyAsync(cancellationToken);
        return healthy
            ? HealthCheckResult.Healthy("Ollama is reachable.")
            : HealthCheckResult.Degraded("Ollama is not reachable. AI-assisted features are unavailable.");
    }
}
