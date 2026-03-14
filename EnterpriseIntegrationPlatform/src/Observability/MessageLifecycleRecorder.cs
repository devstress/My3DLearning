using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Records message lifecycle events into the <see cref="IMessageStateStore"/>
/// and simultaneously emits OpenTelemetry traces and metrics.
/// This is the single entry point that services should call as messages
/// move through the pipeline, ensuring that both the queryable state store
/// and the telemetry pipeline stay in sync.
/// </summary>
public sealed class MessageLifecycleRecorder
{
    private readonly IMessageStateStore _store;
    private readonly ILogger<MessageLifecycleRecorder> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="MessageLifecycleRecorder"/>.
    /// </summary>
    /// <param name="store">The backing state store.</param>
    /// <param name="logger">Logger instance.</param>
    public MessageLifecycleRecorder(IMessageStateStore store, ILogger<MessageLifecycleRecorder> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Records that a message has been received and starts an ingestion trace span.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="businessKey">Optional business key (e.g. order number) for look-up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A started <see cref="Activity"/> span, or <c>null</c>.</returns>
    public async Task<Activity?> RecordReceivedAsync<T>(
        IntegrationEnvelope<T> envelope,
        string? businessKey = null,
        CancellationToken cancellationToken = default)
    {
        var activity = MessageTracer.TraceIngestion(envelope);

        await StoreEventAsync(envelope, MessageTracer.StageIngestion, DeliveryStatus.Pending,
            businessKey, "Message received", cancellationToken);

        _logger.LogInformation(
            "Message {MessageId} received (CorrelationId={CorrelationId}, Type={MessageType}, BusinessKey={BusinessKey})",
            envelope.MessageId, envelope.CorrelationId, envelope.MessageType, businessKey ?? "none");

        return activity;
    }

    /// <summary>
    /// Records that a message has entered a processing stage.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="stage">The stage name (use <see cref="MessageTracer"/> constants).</param>
    /// <param name="businessKey">Optional business key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A started <see cref="Activity"/> span, or <c>null</c>.</returns>
    public async Task<Activity?> RecordProcessingAsync<T>(
        IntegrationEnvelope<T> envelope,
        string stage,
        string? businessKey = null,
        CancellationToken cancellationToken = default)
    {
        var activity = PlatformActivitySource.StartActivity(stage, envelope);
        if (activity is not null)
        {
            TraceEnricher.SetStage(activity, stage);
            TraceEnricher.SetDeliveryStatus(activity, DeliveryStatus.InFlight);
        }

        await StoreEventAsync(envelope, stage, DeliveryStatus.InFlight,
            businessKey, $"Processing in stage: {stage}", cancellationToken);

        _logger.LogInformation(
            "Message {MessageId} processing in {Stage} (CorrelationId={CorrelationId})",
            envelope.MessageId, stage, envelope.CorrelationId);

        return activity;
    }

    /// <summary>
    /// Records that a message has been delivered successfully.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="activity">The activity span to complete (may be <c>null</c>).</param>
    /// <param name="durationMs">Processing duration in milliseconds.</param>
    /// <param name="businessKey">Optional business key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RecordDeliveredAsync<T>(
        IntegrationEnvelope<T> envelope,
        Activity? activity,
        double durationMs,
        string? businessKey = null,
        CancellationToken cancellationToken = default)
    {
        MessageTracer.CompleteSuccess(activity, envelope.MessageType, durationMs);

        await StoreEventAsync(envelope, MessageTracer.StageDelivery, DeliveryStatus.Delivered,
            businessKey, $"Delivered successfully in {durationMs:F1}ms", cancellationToken);

        _logger.LogInformation(
            "Message {MessageId} delivered (CorrelationId={CorrelationId}, Duration={DurationMs}ms)",
            envelope.MessageId, envelope.CorrelationId, durationMs);
    }

    /// <summary>
    /// Records that message processing has failed.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="activity">The activity span to annotate (may be <c>null</c>).</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="stage">The stage where the failure occurred.</param>
    /// <param name="businessKey">Optional business key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RecordFailedAsync<T>(
        IntegrationEnvelope<T> envelope,
        Activity? activity,
        Exception exception,
        string stage,
        string? businessKey = null,
        CancellationToken cancellationToken = default)
    {
        MessageTracer.CompleteFailed(activity, envelope.MessageType, exception);

        await StoreEventAsync(envelope, stage, DeliveryStatus.Failed,
            businessKey, $"Failed: {exception.Message}", cancellationToken);

        _logger.LogError(exception,
            "Message {MessageId} failed in {Stage} (CorrelationId={CorrelationId})",
            envelope.MessageId, stage, envelope.CorrelationId);
    }

    /// <summary>
    /// Records that a message is being retried.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="retryCount">Current retry attempt number.</param>
    /// <param name="stage">The stage where the retry is happening.</param>
    /// <param name="businessKey">Optional business key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RecordRetryAsync<T>(
        IntegrationEnvelope<T> envelope,
        int retryCount,
        string stage,
        string? businessKey = null,
        CancellationToken cancellationToken = default)
    {
        PlatformMeters.RecordRetry(envelope.MessageType, retryCount);

        await StoreEventAsync(envelope, stage, DeliveryStatus.Retrying,
            businessKey, $"Retry attempt #{retryCount}", cancellationToken);

        _logger.LogWarning(
            "Message {MessageId} retrying (attempt #{RetryCount}) in {Stage} (CorrelationId={CorrelationId})",
            envelope.MessageId, retryCount, stage, envelope.CorrelationId);
    }

    /// <summary>
    /// Records that a message has been moved to the dead-letter store.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="reason">Why the message was dead-lettered.</param>
    /// <param name="businessKey">Optional business key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RecordDeadLetteredAsync<T>(
        IntegrationEnvelope<T> envelope,
        string reason,
        string? businessKey = null,
        CancellationToken cancellationToken = default)
    {
        PlatformMeters.RecordDeadLettered(envelope.MessageType);

        await StoreEventAsync(envelope, "DeadLetter", DeliveryStatus.DeadLettered,
            businessKey, $"Dead-lettered: {reason}", cancellationToken);

        _logger.LogError(
            "Message {MessageId} dead-lettered: {Reason} (CorrelationId={CorrelationId})",
            envelope.MessageId, reason, envelope.CorrelationId);
    }

    private async Task StoreEventAsync<T>(
        IntegrationEnvelope<T> envelope,
        string stage,
        DeliveryStatus status,
        string? businessKey,
        string? details,
        CancellationToken cancellationToken)
    {
        var messageEvent = new MessageEvent
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            MessageType = envelope.MessageType,
            Source = envelope.Source,
            Stage = stage,
            Status = status,
            Details = details,
            BusinessKey = businessKey,
            TraceId = envelope.Metadata.GetValueOrDefault(MessageHeaders.TraceId)
                      ?? Activity.Current?.TraceId.ToString(),
            SpanId = envelope.Metadata.GetValueOrDefault(MessageHeaders.SpanId)
                     ?? Activity.Current?.SpanId.ToString(),
        };

        await _store.RecordAsync(messageEvent, cancellationToken);
    }
}
