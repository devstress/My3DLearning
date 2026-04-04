using EnterpriseIntegrationPlatform.AI.Ollama;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Ollama-backed implementation of <see cref="ITraceAnalyzer"/>.
/// Sends structured trace data to an LLM and returns a diagnostic summary
/// that helps operators understand message flow and integration health.
/// </summary>
public sealed class TraceAnalyzer : ITraceAnalyzer
{
    private readonly IOllamaService _ollama;
    private readonly ILogger<TraceAnalyzer> _logger;

    private const string SystemPrompt =
        """
        You are a senior integration engineer analysing an enterprise integration platform.
        The platform processes messages through stages: Ingestion → Routing → Transformation → Delivery.
        Each message carries a CorrelationId for end-to-end tracing and a DeliveryStatus
        (Pending, InFlight, Delivered, Failed, Retrying, DeadLettered).

        When given trace data or message state, provide a concise diagnostic summary:
        1. Identify which stage the message is currently in.
        2. Flag any anomalies (stuck messages, high retry counts, failures).
        3. Suggest next steps if there is a problem.

        Be concise and actionable. Use bullet points.
        """;

    /// <summary>
    /// Initialises a new instance of <see cref="TraceAnalyzer"/>.
    /// </summary>
    /// <param name="ollama">The Ollama service for AI generation.</param>
    /// <param name="logger">Logger instance.</param>
    public TraceAnalyzer(IOllamaService ollama, ILogger<TraceAnalyzer> logger)
    {
        _ollama = ollama;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> AnalyseTraceAsync(
        string traceContextJson,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Requesting AI trace analysis");

        try
        {
            return await _ollama.AnalyseAsync(SystemPrompt, traceContextJson, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Propagate cancellation so callers can detect timeouts and mark
            // Ollama as unavailable (e.g. MessageStateInspector uses a CTS to
            // cap trace analysis time).
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI trace analysis unavailable; returning fallback response");
            return "AI analysis is currently unavailable. Please inspect the trace data manually.";
        }
    }

    /// <inheritdoc />
    public async Task<string> WhereIsMessageAsync(
        Guid correlationId,
        string knownState,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Requesting AI message location analysis for {CorrelationId}", correlationId);

        var prompt = $"Where is the message with CorrelationId={correlationId}? " +
                     $"Current known state:\n{knownState}";

        try
        {
            return await _ollama.AnalyseAsync(SystemPrompt, prompt, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Propagate cancellation so callers can detect timeouts and mark
            // Ollama as unavailable.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI message location analysis unavailable for {CorrelationId}", correlationId);
            return $"AI analysis is currently unavailable for CorrelationId={correlationId}. " +
                   "Please check the trace data manually.";
        }
    }
}
