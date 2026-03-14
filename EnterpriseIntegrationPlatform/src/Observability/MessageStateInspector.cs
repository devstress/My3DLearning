using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Inspects where a message is in the integration pipeline by querying
/// the <see cref="IMessageStateStore"/> and optionally sending the results
/// to <see cref="ITraceAnalyzer"/> (backed by Ollama) for AI-powered diagnostics.
/// <para>
/// This is the primary entry point for operators asking
/// "where is my shipment for order 02?" or
/// "what happened to the invoice with reference INV-2026-0042?".
/// </para>
/// </summary>
/// <example>
/// <code>
/// // Look up by business key (e.g. order number)
/// var result = await inspector.WhereIsAsync("order-02");
///
/// // Look up by correlation ID
/// var result = await inspector.WhereIsByCorrelationAsync(correlationId);
/// </code>
/// </example>
public sealed class MessageStateInspector
{
    private readonly IMessageStateStore _store;
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
    /// <param name="store">The message state store to query.</param>
    /// <param name="traceAnalyzer">The AI-backed trace analyser.</param>
    /// <param name="logger">Logger instance.</param>
    public MessageStateInspector(
        IMessageStateStore store,
        ITraceAnalyzer traceAnalyzer,
        ILogger<MessageStateInspector> logger)
    {
        _store = store;
        _traceAnalyzer = traceAnalyzer;
        _logger = logger;
    }

    /// <summary>
    /// Answers "where is my message?" by looking up the business key
    /// (e.g. order number, shipment ID) in the state store, then sending
    /// the full lifecycle history to Ollama for an AI-generated summary.
    /// </summary>
    /// <param name="businessKey">The business key to search for (e.g. "order-02").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="InspectionResult"/> with both raw state and AI summary.</returns>
    public async Task<InspectionResult> WhereIsAsync(
        string businessKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Looking up message state for business key: {BusinessKey}", businessKey);

        var events = await _store.GetByBusinessKeyAsync(businessKey, cancellationToken);

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
    /// in the state store and sending the history to Ollama for analysis.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="InspectionResult"/> with both raw state and AI summary.</returns>
    public async Task<InspectionResult> WhereIsByCorrelationAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Looking up message state for CorrelationId={CorrelationId}", correlationId);

        var events = await _store.GetByCorrelationIdAsync(correlationId, cancellationToken);

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
    /// Builds a <see cref="MessageStateSnapshot"/> from an <see cref="IntegrationEnvelope{T}"/>
    /// capturing its current known state.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The envelope to inspect.</param>
    /// <param name="currentStage">The current processing stage name.</param>
    /// <param name="deliveryStatus">The current delivery status.</param>
    /// <returns>A snapshot of the message state.</returns>
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

        // Ask the AI for a diagnostic summary
        string aiSummary;
        try
        {
            var correlationId = events[0].CorrelationId;
            aiSummary = await _traceAnalyzer.WhereIsMessageAsync(correlationId, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI analysis unavailable; falling back to structured summary");
            aiSummary = BuildFallbackSummary(query, events, latest);
        }

        return new InspectionResult
        {
            Query = query,
            Found = true,
            Summary = aiSummary,
            Events = events,
            LatestStage = latest.Stage,
            LatestStatus = latest.Status,
        };
    }

    private static string BuildFallbackSummary(
        string query,
        IReadOnlyList<MessageEvent> events,
        MessageEvent latest)
    {
        return $"Message for '{query}' is currently in stage '{latest.Stage}' " +
               $"with status '{latest.Status}'. " +
               $"It has {events.Count} recorded lifecycle event(s). " +
               $"First seen at {events[0].RecordedAt:O}, last update at {latest.RecordedAt:O}.";
    }
}

/// <summary>
/// The result of a message state inspection, including the full lifecycle
/// event history and an AI-generated (or fallback) diagnostic summary.
/// </summary>
public sealed class InspectionResult
{
    /// <summary>The original query (business key or correlation ID).</summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>Whether any matching events were found.</summary>
    public bool Found { get; init; }

    /// <summary>AI-generated or fallback diagnostic summary.</summary>
    public string Summary { get; init; } = string.Empty;

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
