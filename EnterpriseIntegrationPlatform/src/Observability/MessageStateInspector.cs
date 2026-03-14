using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Inspects where a message is in the integration pipeline by combining
/// envelope metadata with AI-powered trace analysis from <see cref="ITraceAnalyzer"/>.
/// This is the primary entry point for operators asking
/// "where is my message?" or "what is the state of this integration?".
/// </summary>
public sealed class MessageStateInspector
{
    private readonly ITraceAnalyzer _traceAnalyzer;
    private readonly ILogger<MessageStateInspector> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="MessageStateInspector"/>.
    /// </summary>
    /// <param name="traceAnalyzer">The AI-backed trace analyser.</param>
    /// <param name="logger">Logger instance.</param>
    public MessageStateInspector(ITraceAnalyzer traceAnalyzer, ILogger<MessageStateInspector> logger)
    {
        _traceAnalyzer = traceAnalyzer;
        _logger = logger;
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
    public MessageStateSnapshot Inspect<T>(
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

    /// <summary>
    /// Asks the AI analyser where a message is in the pipeline, given its
    /// current <see cref="MessageStateSnapshot"/>.
    /// </summary>
    /// <param name="snapshot">The known state of the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An AI-generated summary of the message location.</returns>
    public async Task<string> WhereIsAsync(
        MessageStateSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Inspecting message state for CorrelationId={CorrelationId}, Stage={Stage}, Status={Status}",
            snapshot.CorrelationId, snapshot.CurrentStage, snapshot.DeliveryStatus);

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        return await _traceAnalyzer.WhereIsMessageAsync(snapshot.CorrelationId, json, cancellationToken);
    }
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
