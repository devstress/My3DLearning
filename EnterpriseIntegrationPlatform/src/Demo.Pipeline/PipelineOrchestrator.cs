using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Observability;
using EnterpriseIntegrationPlatform.Storage.Cassandra;

namespace EnterpriseIntegrationPlatform.Demo.Pipeline;

/// <summary>
/// Processes a single inbound <see cref="IntegrationEnvelope{T}"/> through the full
/// end-to-end demo pipeline:
/// <list type="number">
///   <item>Persist the inbound message to Cassandra as <see cref="DeliveryStatus.Pending"/>.</item>
///   <item>Record a <em>Received</em> lifecycle event via <see cref="MessageLifecycleRecorder"/>.</item>
///   <item>Dispatch to the Temporal <c>ProcessIntegrationMessageWorkflow</c> and await the result.</item>
///   <item>On success — update Cassandra to <see cref="DeliveryStatus.Delivered"/>, record a
///         <em>Delivered</em> event, publish an Ack to the configured NATS subject.</item>
///   <item>On validation failure or exception — update Cassandra to <see cref="DeliveryStatus.Failed"/>,
///         persist a <see cref="FaultEnvelope"/>, record a <em>Failed</em> event, publish a Nack.</item>
/// </list>
/// Every accepted message is therefore either delivered or permanently recorded as a fault —
/// satisfying the Zero Message Loss quality pillar.
/// </summary>
public sealed class PipelineOrchestrator : IPipelineOrchestrator
{
    private const string ServiceName = "Demo.Pipeline";

    private readonly IMessageRepository _repository;
    private readonly MessageLifecycleRecorder _lifecycle;
    private readonly IMessageBrokerProducer _producer;
    private readonly ITemporalWorkflowDispatcher _dispatcher;
    private readonly PipelineOptions _options;
    private readonly ILogger<PipelineOrchestrator> _logger;

    /// <summary>Initialises a new instance of <see cref="PipelineOrchestrator"/>.</summary>
    public PipelineOrchestrator(
        IMessageRepository repository,
        MessageLifecycleRecorder lifecycle,
        IMessageBrokerProducer producer,
        ITemporalWorkflowDispatcher dispatcher,
        IOptions<PipelineOptions> options,
        ILogger<PipelineOrchestrator> logger)
    {
        _repository = repository;
        _lifecycle = lifecycle;
        _producer = producer;
        _dispatcher = dispatcher;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ProcessAsync(
        IntegrationEnvelope<JsonElement> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var sw = Stopwatch.StartNew();

        // Step 1 — Persist inbound message to Cassandra
        var payloadJson = envelope.Payload.GetRawText();
        var record = new MessageRecord
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            CausationId = envelope.CausationId,
            RecordedAt = envelope.Timestamp,
            Source = envelope.Source,
            MessageType = envelope.MessageType,
            SchemaVersion = envelope.SchemaVersion,
            Priority = envelope.Priority,
            PayloadJson = payloadJson,
            DeliveryStatus = DeliveryStatus.Pending,
        };

        await _repository.SaveMessageAsync(record, cancellationToken);

        // Step 2 — Record lifecycle event (Received)
        Activity? activity = null;
        try
        {
            activity = await _lifecycle.RecordReceivedAsync(envelope, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Lifecycle recorder failed for message {MessageId} — continuing",
                envelope.MessageId);
        }

        // Step 3 — Dispatch to Temporal workflow
        ProcessIntegrationMessageResult workflowResult;
        try
        {
            var input = new ProcessIntegrationMessageInput(
                envelope.MessageId,
                envelope.MessageType,
                payloadJson);

            var workflowId = $"integration-{envelope.MessageId}";

            workflowResult = await _dispatcher.DispatchAsync(input, workflowId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await HandleFailureAsync(envelope, record, activity, ex,
                "Temporal workflow dispatch failed", sw.Elapsed.TotalMilliseconds,
                cancellationToken);
            return;
        }

        sw.Stop();
        var durationMs = sw.Elapsed.TotalMilliseconds;

        if (workflowResult.IsValid)
        {
            await HandleSuccessAsync(envelope, record, activity, durationMs, cancellationToken);
        }
        else
        {
            var reason = workflowResult.FailureReason ?? "Validation failed";
            await HandleValidationFailureAsync(
                envelope, record, activity, reason, durationMs, cancellationToken);
        }
    }

    private async Task HandleSuccessAsync(
        IntegrationEnvelope<JsonElement> envelope,
        MessageRecord record,
        Activity? activity,
        double durationMs,
        CancellationToken ct)
    {
        // Update Cassandra delivery status
        await _repository.UpdateDeliveryStatusAsync(
            envelope.MessageId, envelope.CorrelationId,
            record.RecordedAt, DeliveryStatus.Delivered, ct);

        // Record Delivered lifecycle event
        try
        {
            await _lifecycle.RecordDeliveredAsync(envelope, activity, durationMs, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Lifecycle recorder (Delivered) failed for message {MessageId} — continuing",
                envelope.MessageId);
        }

        // Publish Ack
        var ack = IntegrationEnvelope<AckPayload>.Create(
            new AckPayload(envelope.MessageId, envelope.CorrelationId, "Delivered"),
            source: ServiceName,
            messageType: "Integration.Ack",
            correlationId: envelope.CorrelationId,
            causationId: envelope.MessageId);

        await _producer.PublishAsync(ack, _options.AckSubject, ct);

        _logger.LogInformation(
            "Message {MessageId} processed successfully in {DurationMs:F1}ms — Ack published",
            envelope.MessageId, durationMs);
    }

    private async Task HandleValidationFailureAsync(
        IntegrationEnvelope<JsonElement> envelope,
        MessageRecord record,
        Activity? activity,
        string reason,
        double durationMs,
        CancellationToken ct)
    {
        // Update Cassandra delivery status
        await _repository.UpdateDeliveryStatusAsync(
            envelope.MessageId, envelope.CorrelationId,
            record.RecordedAt, DeliveryStatus.Failed, ct);

        // Persist fault
        var fault = FaultEnvelope.Create(
            envelope,
            faultedBy: ServiceName,
            reason: reason,
            retryCount: 0);

        await _repository.SaveFaultAsync(fault, ct);

        // Record Failed lifecycle event
        var validationException = new InvalidOperationException(reason);
        try
        {
            await _lifecycle.RecordFailedAsync(
                envelope, activity, validationException,
                stage: "Validation", cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Lifecycle recorder (Failed) failed for message {MessageId} — continuing",
                envelope.MessageId);
        }

        // Publish Nack
        await PublishNackAsync(envelope, reason, ct);

        _logger.LogWarning(
            "Message {MessageId} failed validation in {DurationMs:F1}ms: {Reason} — Nack published",
            envelope.MessageId, durationMs, reason);
    }

    private async Task HandleFailureAsync(
        IntegrationEnvelope<JsonElement> envelope,
        MessageRecord record,
        Activity? activity,
        Exception exception,
        string reason,
        double durationMs,
        CancellationToken ct)
    {
        // Update Cassandra delivery status
        try
        {
            await _repository.UpdateDeliveryStatusAsync(
                envelope.MessageId, envelope.CorrelationId,
                record.RecordedAt, DeliveryStatus.Failed, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not update Cassandra status for failed message {MessageId}",
                envelope.MessageId);
        }

        // Persist fault
        try
        {
            var fault = FaultEnvelope.Create(
                envelope,
                faultedBy: ServiceName,
                reason: reason,
                retryCount: 0,
                exception: exception);

            await _repository.SaveFaultAsync(fault, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not save fault envelope for message {MessageId}",
                envelope.MessageId);
        }

        // Record Failed lifecycle event
        try
        {
            await _lifecycle.RecordFailedAsync(
                envelope, activity, exception,
                stage: "Processing", cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Lifecycle recorder (Failed) failed for message {MessageId} — continuing",
                envelope.MessageId);
        }

        // Publish Nack
        await PublishNackAsync(envelope, reason, ct);

        _logger.LogError(exception,
            "Message {MessageId} failed in {DurationMs:F1}ms: {Reason} — Nack published",
            envelope.MessageId, durationMs, reason);
    }

    private Task PublishNackAsync(
        IntegrationEnvelope<JsonElement> envelope,
        string reason,
        CancellationToken ct)
    {
        var nack = IntegrationEnvelope<NackPayload>.Create(
            new NackPayload(envelope.MessageId, envelope.CorrelationId, reason),
            source: ServiceName,
            messageType: "Integration.Nack",
            correlationId: envelope.CorrelationId,
            causationId: envelope.MessageId);

        return _producer.PublishAsync(nack, _options.NackSubject, ct);
    }
}
