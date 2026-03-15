using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;

namespace EnterpriseIntegrationPlatform.Demo.Pipeline;

/// <summary>
/// Long-running <see cref="BackgroundService"/> that subscribes to the configured
/// NATS JetStream inbound subject and hands each received message to
/// <see cref="IPipelineOrchestrator"/> for end-to-end processing.
/// <para>
/// The worker runs until the host is shut down. Each message is processed
/// in-order within the consumer group; because NATS JetStream uses per-subject
/// delivery, a slow message on one subject does not block messages on other subjects.
/// </para>
/// </summary>
public sealed class IntegrationPipelineWorker : BackgroundService
{
    private readonly IMessageBrokerConsumer _consumer;
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly PipelineOptions _options;
    private readonly ILogger<IntegrationPipelineWorker> _logger;

    /// <summary>Initialises a new instance of <see cref="IntegrationPipelineWorker"/>.</summary>
    public IntegrationPipelineWorker(
        IMessageBrokerConsumer consumer,
        IPipelineOrchestrator orchestrator,
        IOptions<PipelineOptions> options,
        ILogger<IntegrationPipelineWorker> logger)
    {
        _consumer = consumer;
        _orchestrator = orchestrator;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "IntegrationPipelineWorker starting — subscribing to '{Subject}' (group '{Group}')",
            _options.InboundSubject, _options.ConsumerGroup);

        try
        {
            await _consumer.SubscribeAsync<JsonElement>(
                topic: _options.InboundSubject,
                consumerGroup: _options.ConsumerGroup,
                handler: envelope => HandleMessageAsync(envelope, stoppingToken),
                cancellationToken: stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path — host is stopping
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "IntegrationPipelineWorker encountered an unrecoverable error on subject '{Subject}'",
                _options.InboundSubject);

            // Re-throw so Aspire / the process supervisor can restart the service
            throw;
        }

        _logger.LogInformation("IntegrationPipelineWorker stopped");
    }

    private async Task HandleMessageAsync(
        IntegrationEnvelope<JsonElement> envelope,
        CancellationToken ct)
    {
        _logger.LogDebug(
            "Received message {MessageId} (type={MessageType}, correlation={CorrelationId})",
            envelope.MessageId, envelope.MessageType, envelope.CorrelationId);

        try
        {
            await _orchestrator.ProcessAsync(envelope, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The orchestrator already handles failures internally (fault persistence,
            // Nack publishing, and lifecycle recording). If it throws here it is an
            // unexpected error — log it and continue consuming so the worker stays alive.
            _logger.LogError(ex,
                "Unexpected error processing message {MessageId} — message will not be re-delivered",
                envelope.MessageId);
        }
    }
}
