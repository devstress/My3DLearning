using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Production implementation of the Control Bus Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// Publishes control commands to a dedicated control topic and subscribes to
/// incoming control messages. This formalizes the <c>Admin.Api</c> endpoints
/// as part of the Control Bus pattern — admin REST calls result in control
/// messages being published to the bus, and platform services subscribe to
/// apply runtime configuration changes.
/// </para>
/// </remarks>
public sealed class ControlBusPublisher : IControlBus
{
    private readonly IMessageBrokerProducer _producer;
    private readonly IMessageBrokerConsumer _consumer;
    private readonly ControlBusOptions _options;
    private readonly ILogger<ControlBusPublisher> _logger;

    /// <summary>Initialises a new instance of <see cref="ControlBusPublisher"/>.</summary>
    public ControlBusPublisher(
        IMessageBrokerProducer producer,
        IMessageBrokerConsumer consumer,
        IOptions<ControlBusOptions> options,
        ILogger<ControlBusPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _consumer = consumer;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ControlBusResult> PublishCommandAsync<T>(
        T command,
        string commandType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);

        try
        {
            var envelope = IntegrationEnvelope<T>.Create(
                command,
                source: _options.Source,
                messageType: commandType) with
            {
                Intent = MessageIntent.Command,
            };

            await _producer.PublishAsync(envelope, _options.ControlTopic, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Control command '{CommandType}' published to '{ControlTopic}' (MessageId={MessageId})",
                commandType, _options.ControlTopic, envelope.MessageId);

            return new ControlBusResult(Succeeded: true, ControlTopic: _options.ControlTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish control command '{CommandType}' to '{ControlTopic}'",
                commandType, _options.ControlTopic);

            return new ControlBusResult(Succeeded: false, ControlTopic: _options.ControlTopic,
                FailureReason: ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(
        string commandType,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        ArgumentNullException.ThrowIfNull(handler);

        _logger.LogInformation(
            "Subscribing to control commands of type '{CommandType}' on '{ControlTopic}'",
            commandType, _options.ControlTopic);

        await _consumer.SubscribeAsync<T>(
            _options.ControlTopic,
            _options.ConsumerGroup,
            async envelope =>
            {
                if (string.Equals(envelope.MessageType, commandType, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(
                        "Control command '{CommandType}' received (MessageId={MessageId})",
                        commandType, envelope.MessageId);

                    await handler(envelope).ConfigureAwait(false);
                }
            },
            cancellationToken).ConfigureAwait(false);
    }
}
