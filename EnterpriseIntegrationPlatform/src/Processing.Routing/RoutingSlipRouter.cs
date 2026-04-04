using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Production implementation of the Routing Slip EIP pattern.
/// </summary>
/// <remarks>
/// <para>
/// Reads the <see cref="RoutingSlip"/> from the envelope's metadata, resolves the
/// handler for the current step, executes it, advances the slip, and optionally
/// forwards the message to the step's destination topic.
/// </para>
/// <para>
/// Step handlers are resolved from the DI container via <see cref="IRoutingSlipStepHandler"/>.
/// If no handler is registered for a step name, the step fails.
/// </para>
/// </remarks>
public sealed class RoutingSlipRouter : IRoutingSlipRouter
{
    private readonly IEnumerable<IRoutingSlipStepHandler> _handlers;
    private readonly IMessageBrokerProducer _producer;
    private readonly ILogger<RoutingSlipRouter> _logger;

    /// <summary>Initialises a new instance of <see cref="RoutingSlipRouter"/>.</summary>
    public RoutingSlipRouter(
        IEnumerable<IRoutingSlipStepHandler> handlers,
        IMessageBrokerProducer producer,
        ILogger<RoutingSlipRouter> logger)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(logger);

        _handlers = handlers;
        _producer = producer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RoutingSlipStepResult> ExecuteCurrentStepAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var slip = DeserializeSlip(envelope);

        if (slip.IsComplete)
        {
            throw new InvalidOperationException(
                $"Routing slip on message {envelope.MessageId} is already complete — no steps remain.");
        }

        var currentStep = slip.CurrentStep!;
        var handler = ResolveHandler(currentStep.StepName);

        if (handler is null)
        {
            _logger.LogWarning(
                "No handler registered for routing slip step '{StepName}' on message {MessageId}",
                currentStep.StepName, envelope.MessageId);

            return new RoutingSlipStepResult(
                StepName: currentStep.StepName,
                Succeeded: false,
                FailureReason: $"No handler registered for step '{currentStep.StepName}'",
                RemainingSlip: slip,
                ForwardedToTopic: null);
        }

        bool success;
        try
        {
            success = await handler.HandleAsync(envelope, currentStep.Parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Routing slip step '{StepName}' failed on message {MessageId}",
                currentStep.StepName, envelope.MessageId);

            return new RoutingSlipStepResult(
                StepName: currentStep.StepName,
                Succeeded: false,
                FailureReason: ex.Message,
                RemainingSlip: slip,
                ForwardedToTopic: null);
        }

        if (!success)
        {
            _logger.LogWarning(
                "Routing slip step '{StepName}' returned failure for message {MessageId}",
                currentStep.StepName, envelope.MessageId);

            return new RoutingSlipStepResult(
                StepName: currentStep.StepName,
                Succeeded: false,
                FailureReason: $"Step '{currentStep.StepName}' returned failure",
                RemainingSlip: slip,
                ForwardedToTopic: null);
        }

        var advancedSlip = slip.Advance();

        // Persist the advanced slip back into the envelope metadata
        UpdateSlipMetadata(envelope, advancedSlip);

        // Forward to destination topic if specified
        string? forwardedTo = null;
        if (!string.IsNullOrWhiteSpace(currentStep.DestinationTopic))
        {
            await _producer.PublishAsync(envelope, currentStep.DestinationTopic, cancellationToken);
            forwardedTo = currentStep.DestinationTopic;

            _logger.LogDebug(
                "Message {MessageId} forwarded to '{Topic}' after step '{StepName}'",
                envelope.MessageId, currentStep.DestinationTopic, currentStep.StepName);
        }
        else
        {
            _logger.LogDebug(
                "Step '{StepName}' completed in-process for message {MessageId}; {Remaining} steps remaining",
                currentStep.StepName, envelope.MessageId, advancedSlip.Steps.Count);
        }

        return new RoutingSlipStepResult(
            StepName: currentStep.StepName,
            Succeeded: true,
            FailureReason: null,
            RemainingSlip: advancedSlip,
            ForwardedToTopic: forwardedTo);
    }

    private static RoutingSlip DeserializeSlip<T>(IntegrationEnvelope<T> envelope)
    {
        if (!envelope.Metadata.TryGetValue(RoutingSlip.MetadataKey, out var slipJson))
        {
            throw new InvalidOperationException(
                $"Message {envelope.MessageId} does not contain a routing slip in metadata key '{RoutingSlip.MetadataKey}'.");
        }

        var steps = JsonSerializer.Deserialize<List<RoutingSlipStep>>(slipJson, JsonOptions)
                    ?? [];

        return new RoutingSlip(steps.AsReadOnly());
    }

    private static void UpdateSlipMetadata<T>(IntegrationEnvelope<T> envelope, RoutingSlip advancedSlip)
    {
        envelope.Metadata[RoutingSlip.MetadataKey] =
            JsonSerializer.Serialize(advancedSlip.Steps, JsonOptions);
    }

    private IRoutingSlipStepHandler? ResolveHandler(string stepName) =>
        _handlers.FirstOrDefault(h =>
            string.Equals(h.StepName, stepName, StringComparison.OrdinalIgnoreCase));

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };
}
