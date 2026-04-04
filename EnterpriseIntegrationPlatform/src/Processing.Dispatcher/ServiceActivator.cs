using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Dispatcher;

/// <summary>
/// Production implementation of the Service Activator Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// The Service Activator connects messaging infrastructure to application services.
/// It invokes a service operation from an inbound message and, when the envelope
/// specifies a <see cref="IntegrationEnvelope{T}.ReplyTo"/> address and the service
/// produces a response, publishes the reply to that address.
/// </para>
/// <para>
/// This pattern is key for request-reply orchestration — the inbound channel
/// delivers the request, the Service Activator invokes the business service, and
/// the reply is routed back via the messaging layer.
/// </para>
/// </remarks>
public sealed class ServiceActivator : IServiceActivator
{
    private readonly IMessageBrokerProducer _producer;
    private readonly ServiceActivatorOptions _options;
    private readonly ILogger<ServiceActivator> _logger;

    /// <summary>Initialises a new instance of <see cref="ServiceActivator"/>.</summary>
    public ServiceActivator(
        IMessageBrokerProducer producer,
        IOptions<ServiceActivatorOptions> options,
        ILogger<ServiceActivator> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ServiceActivatorResult> InvokeAsync<TRequest, TResponse>(
        IntegrationEnvelope<TRequest> envelope,
        Func<IntegrationEnvelope<TRequest>, CancellationToken, Task<TResponse?>> serviceOperation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(serviceOperation);

        TResponse? response;
        try
        {
            response = await serviceOperation(envelope, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Let cancellation propagate
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Service operation failed for message {MessageId} (type={MessageType})",
                envelope.MessageId, envelope.MessageType);

            return new ServiceActivatorResult(Succeeded: false, ReplySent: false,
                FailureReason: ex.Message);
        }

        _logger.LogDebug(
            "Service operation completed for message {MessageId} (type={MessageType})",
            envelope.MessageId, envelope.MessageType);

        // If there's a reply and a ReplyTo address, publish the reply
        if (response is not null && !string.IsNullOrWhiteSpace(envelope.ReplyTo))
        {
            var replyEnvelope = IntegrationEnvelope<TResponse>.Create(
                response,
                source: _options.ReplySource,
                messageType: _options.ReplyMessageType,
                correlationId: envelope.CorrelationId,
                causationId: envelope.MessageId);

            await _producer.PublishAsync(replyEnvelope, envelope.ReplyTo, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "Reply published for message {MessageId} to '{ReplyTo}' (CorrelationId={CorrelationId})",
                envelope.MessageId, envelope.ReplyTo, envelope.CorrelationId);

            return new ServiceActivatorResult(Succeeded: true, ReplySent: true,
                ReplyTopic: envelope.ReplyTo);
        }

        // Service succeeded, no reply needed or no ReplyTo address
        if (response is not null && string.IsNullOrWhiteSpace(envelope.ReplyTo))
        {
            _logger.LogDebug(
                "Service produced a response for message {MessageId} but no ReplyTo address is set — reply discarded",
                envelope.MessageId);
        }

        return new ServiceActivatorResult(Succeeded: true, ReplySent: false);
    }

    /// <inheritdoc />
    public async Task<ServiceActivatorResult> InvokeAsync<T>(
        IntegrationEnvelope<T> envelope,
        Func<IntegrationEnvelope<T>, CancellationToken, Task> serviceOperation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(serviceOperation);

        try
        {
            await serviceOperation(envelope, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Let cancellation propagate
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Service operation (fire-and-forget) failed for message {MessageId} (type={MessageType})",
                envelope.MessageId, envelope.MessageType);

            return new ServiceActivatorResult(Succeeded: false, ReplySent: false,
                FailureReason: ex.Message);
        }

        _logger.LogDebug(
            "Service operation (fire-and-forget) completed for message {MessageId} (type={MessageType})",
            envelope.MessageId, envelope.MessageType);

        return new ServiceActivatorResult(Succeeded: true, ReplySent: false);
    }
}
