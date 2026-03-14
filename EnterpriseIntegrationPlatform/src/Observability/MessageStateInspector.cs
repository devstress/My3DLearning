using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Inspects where a message is in the integration pipeline by querying
/// the isolated <see cref="IObservabilityEventLog"/> (NOT the production
/// <see cref="IMessageStateStore"/>) and optionally sending the results
/// to <see cref="ITraceAnalyzer"/> (backed by Ollama) for AI-powered diagnostics.
/// <para>
/// This is the primary entry point for operators asking
/// "where is my shipment for order 02?" via OpenClaw.
/// Observability queries are always served from the isolated observability
/// storage (backed by Prometheus for metrics + event log for lifecycle events).
/// </para>
/// </summary>
public sealed class MessageStateInspector
{
    private readonly IObservabilityEventLog _observabilityLog;
    private readonly ITraceAnalyzer _traceAnalyzer;
    private readonly ILogger<MessageStateInspector> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Initialises a new instance of <see cref="MessageStateInspector"/>.
    /// </summary>
    /// <param name="observabilityLog">The isolated observability event log to query.</param>
    /// <param name="traceAnalyzer">The AI-backed trace analyser.</param>
    /// <param name="logger">Logger instance.</param>
    public MessageStateInspector(
        IObservabilityEventLog observabilityLog,
        ITraceAnalyzer traceAnalyzer,
        ILogger<MessageStateInspector> logger)
    {
        _observabilityLog = observabilityLog;
        _traceAnalyzer = traceAnalyzer;
        _logger = logger;
    }

    /// <summary>
    /// Answers "where is my message?" by looking up the business key
    /// in the isolated observability event log, then sending the full
    /// lifecycle history to Ollama for an AI-generated summary.
    /// When Ollama is unavailable the result carries an explicit notification.
    /// </summary>
    public async Task<InspectionResult> WhereIsAsync(
        string businessKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Looking up observability data for business key: {BusinessKey}", businessKey);

        var events = await _observabilityLog.GetByBusinessKeyAsync(businessKey, cancellationToken);

        if (events.Count == 0)
        {
            return new InspectionResult
            {
                Query = businessKey,
                Found = false,
                Summary = $"No messages found for business key '{businessKey}'.",
                Events = [],
            };
        }

        return await BuildResultAsync(businessKey, events, cancellationToken);
    }

    /// <summary>
    /// Answers "where is my message?" by looking up a correlation identifier
    /// in the isolated observability event log and sending the history to Ollama.
    /// When Ollama is unavailable the result carries an explicit notification.
    /// </summary>
    public async Task<InspectionResult> WhereIsByCorrelationAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Looking up observability data for CorrelationId={CorrelationId}", correlationId);

        var events = await _observabilityLog.GetByCorrelationIdAsync(correlationId, cancellationToken);

        if (events.Count == 0)
        {
            return new InspectionResult
            {
                Query = correlationId.ToString(),
                Found = false,
                Summary = $"No messages found for CorrelationId={correlationId}.",
                Events = [],
            };
        }

        return await BuildResultAsync(correlationId.ToString(), events, cancellationToken);
    }

    /// <summary>
    /// Builds a <see cref="MessageStateSnapshot"/> from an <see cref="IntegrationEnvelope{T}"/>.
    /// </summary>
    public MessageStateSnapshot CreateSnapshot<T>(
        IntegrationEnvelope<T> envelope,
        string currentStage,
        DeliveryStatus deliveryStatus)
    {
        return new MessageStateSnapshot
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            CausationId = envelope.CausationId,
            MessageType = envelope.MessageType,
            Source = envelope.Source,
            Priority = envelope.Priority,
            Timestamp = envelope.Timestamp,
            CurrentStage = currentStage,
            DeliveryStatus = deliveryStatus,
            TraceId = envelope.Metadata.GetValueOrDefault(MessageHeaders.TraceId),
            SpanId = envelope.Metadata.GetValueOrDefault(MessageHeaders.SpanId),
            RetryCount = envelope.Metadata.TryGetValue(MessageHeaders.RetryCount, out var rc)
                ? int.TryParse(rc, out var count) ? count : 0
                : 0,
        };
    }

    private async Task<InspectionResult> BuildResultAsync(
        string query,
        IReadOnlyList<MessageEvent> events,
        CancellationToken cancellationToken)
    {
        var latest = events[^1];
        var json = JsonSerializer.Serialize(events, JsonOptions);

        string aiSummary;
        bool ollamaAvailable = true;
        try
        {
            var correlationId = events[0].CorrelationId;
            aiSummary = await _traceAnalyzer.WhereIsMessageAsync(correlationId, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama is unavailable for AI analysis");
            aiSummary = "⚠️ Ollama is unavailable. AI-powered analysis cannot be performed at this time. " +
                        "Please ensure Ollama is running and accessible. " +
                        "Lifecycle event data is still available below from the observability store.";
            ollamaAvailable = false;
        }

        return new InspectionResult
        {
            Query = query,
            Found = true,
            Summary = aiSummary,
            OllamaAvailable = ollamaAvailable,
            Events = events,
            LatestStage = latest.Stage,
            LatestStatus = latest.Status,
        };
    }
}

/// <summary>
/// The result of a message state inspection, including the full lifecycle
/// event history and an AI-generated diagnostic summary (or an explicit
/// notification that Ollama is unavailable).
/// </summary>
public sealed class InspectionResult
{
    /// <summary>The original query (business key or correlation ID).</summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>Whether any matching events were found.</summary>
    public bool Found { get; init; }

    /// <summary>
    /// AI-generated diagnostic summary, or an explicit notification
    /// that Ollama is unavailable.
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// <c>true</c> when the AI summary was generated by Ollama;
    /// <c>false</c> when Ollama was unavailable and the summary
    /// is a notification instead.
    /// </summary>
    public bool OllamaAvailable { get; init; } = true;

    /// <summary>The ordered list of lifecycle events.</summary>
    public IReadOnlyList<MessageEvent> Events { get; init; } = [];

    /// <summary>The most recent processing stage, if known.</summary>
    public string? LatestStage { get; init; }

    /// <summary>The most recent delivery status, if known.</summary>
    public DeliveryStatus? LatestStatus { get; init; }
}

/// <summary>
/// A point-in-time snapshot of a message's state as it flows through the platform.
/// </summary>
public sealed class MessageStateSnapshot
{
    /// <summary>Unique message identifier.</summary>
    public Guid MessageId { get; init; }

    /// <summary>Correlation identifier for end-to-end tracing.</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>Causation identifier, if present.</summary>
    public Guid? CausationId { get; init; }

    /// <summary>Logical message type.</summary>
    public string MessageType { get; init; } = string.Empty;

    /// <summary>Originating source.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Message priority.</summary>
    public MessagePriority Priority { get; init; }

    /// <summary>Original creation timestamp.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Current processing stage.</summary>
    public string CurrentStage { get; init; } = string.Empty;

    /// <summary>Current delivery status.</summary>
    public DeliveryStatus DeliveryStatus { get; init; }

    /// <summary>W3C trace identifier, if propagated.</summary>
    public string? TraceId { get; init; }

    /// <summary>W3C span identifier, if propagated.</summary>
    public string? SpanId { get; init; }

    /// <summary>Number of retry attempts.</summary>
    public int RetryCount { get; init; }
}
