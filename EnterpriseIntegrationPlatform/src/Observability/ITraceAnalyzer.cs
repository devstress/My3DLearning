namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Provides trace analysis of message flow and integration state.
/// Implementations use the Ollama LLM to reason about
/// where a message is in the pipeline and whether processing is healthy.
/// </summary>
public interface ITraceAnalyzer
{
    /// <summary>
    /// Analyses a structured trace snapshot and returns a natural-language diagnostic summary.
    /// </summary>
    /// <param name="traceContextJson">
    /// A JSON string containing trace spans, message metadata, and delivery status.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A trace analysis summary.</returns>
    Task<string> AnalyseTraceAsync(string traceContextJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the AI model where a specific message is based on its current
    /// correlation identifier and known trace data.
    /// </summary>
    /// <param name="correlationId">The correlation identifier of the message.</param>
    /// <param name="knownState">Known state information as a JSON payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A natural-language description of the message's current location.</returns>
    Task<string> WhereIsMessageAsync(Guid correlationId, string knownState, CancellationToken cancellationToken = default);
}
