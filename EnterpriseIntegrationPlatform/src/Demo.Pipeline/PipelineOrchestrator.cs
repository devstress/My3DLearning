using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Demo.Pipeline;

/// <summary>
/// Thin dispatcher that converts an inbound <see cref="IntegrationEnvelope{T}"/>
/// into an <see cref="IntegrationPipelineInput"/> and dispatches it to the Temporal
/// <c>IntegrationPipelineWorkflow</c>. All orchestration logic (persist, validate,
/// ack/nack) now runs atomically inside Temporal — this class is intentionally
/// minimal.
/// <para>
/// <b>Atomic all-or-nothing:</b> The Temporal workflow handles every side-effect
/// as a durable activity. If any step fails, Temporal retries. If the process crashes,
/// the workflow resumes from the last completed activity. No partial state.
/// </para>
/// </summary>
public sealed class PipelineOrchestrator : IPipelineOrchestrator
{
    private readonly ITemporalWorkflowDispatcher _dispatcher;
    private readonly PipelineOptions _options;
    private readonly ILogger<PipelineOrchestrator> _logger;

    /// <summary>Initialises a new instance of <see cref="PipelineOrchestrator"/>.</summary>
    public PipelineOrchestrator(
        ITemporalWorkflowDispatcher dispatcher,
        IOptions<PipelineOptions> options,
        ILogger<PipelineOrchestrator> logger)
    {
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

        var payloadJson = envelope.Payload.GetRawText();
        var metadataJson = envelope.Metadata.Count > 0
            ? JsonSerializer.Serialize(envelope.Metadata)
            : null;

        var input = new IntegrationPipelineInput(
            MessageId: envelope.MessageId,
            CorrelationId: envelope.CorrelationId,
            CausationId: envelope.CausationId,
            Timestamp: envelope.Timestamp,
            Source: envelope.Source,
            MessageType: envelope.MessageType,
            SchemaVersion: envelope.SchemaVersion,
            Priority: (int)envelope.Priority,
            PayloadJson: payloadJson,
            MetadataJson: metadataJson,
            AckSubject: _options.AckSubject,
            NackSubject: _options.NackSubject);

        var workflowId = $"integration-{envelope.MessageId}";

        _logger.LogDebug(
            "Dispatching message {MessageId} to IntegrationPipelineWorkflow",
            envelope.MessageId);

        var result = await _dispatcher.DispatchAsync(input, workflowId, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Message {MessageId} processed successfully via Temporal workflow",
                envelope.MessageId);
        }
        else
        {
            _logger.LogWarning(
                "Message {MessageId} failed via Temporal workflow: {Reason}",
                envelope.MessageId, result.FailureReason);
        }
    }
}
