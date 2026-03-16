using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Translator;

/// <summary>
/// Production implementation of the Message Translator Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// The translator delegates payload transformation to an injected
/// <see cref="IPayloadTransform{TIn,TOut}"/>, then wraps the result in a new
/// <see cref="IntegrationEnvelope{TOut}"/> that preserves the source envelope's
/// <c>CorrelationId</c>, <c>Priority</c>, <c>SchemaVersion</c>, and <c>Metadata</c>.
/// The <c>CausationId</c> of the translated envelope is set to the source
/// <see cref="IntegrationEnvelope{T}.MessageId"/> to maintain the full causation chain.
/// </para>
/// <para>
/// The translated envelope is published to <see cref="TranslatorOptions.TargetTopic"/>
/// via the registered <see cref="IMessageBrokerProducer"/>.
/// </para>
/// </remarks>
/// <typeparam name="TIn">Source payload type.</typeparam>
/// <typeparam name="TOut">Target payload type.</typeparam>
public sealed class MessageTranslator<TIn, TOut> : IMessageTranslator<TIn, TOut>
{
    private readonly IPayloadTransform<TIn, TOut> _transform;
    private readonly IMessageBrokerProducer _producer;
    private readonly TranslatorOptions _options;
    private readonly ILogger<MessageTranslator<TIn, TOut>> _logger;

    /// <summary>Initialises a new instance of <see cref="MessageTranslator{TIn,TOut}"/>.</summary>
    public MessageTranslator(
        IPayloadTransform<TIn, TOut> transform,
        IMessageBrokerProducer producer,
        IOptions<TranslatorOptions> options,
        ILogger<MessageTranslator<TIn, TOut>> logger)
    {
        _transform = transform;
        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TranslationResult<TOut>> TranslateAsync(
        IntegrationEnvelope<TIn> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.IsNullOrWhiteSpace(_options.TargetTopic))
            throw new InvalidOperationException(
                $"MessageTranslator: TargetTopic is not configured. " +
                $"Envelope {source.MessageId} (type='{source.MessageType}') cannot be translated.");

        var translatedPayload = _transform.Transform(source.Payload);

        var translated = new IntegrationEnvelope<TOut>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = source.CorrelationId,
            CausationId = source.MessageId,
            Timestamp = DateTimeOffset.UtcNow,
            Source = string.IsNullOrWhiteSpace(_options.TargetSource)
                ? source.Source
                : _options.TargetSource,
            MessageType = string.IsNullOrWhiteSpace(_options.TargetMessageType)
                ? source.MessageType
                : _options.TargetMessageType,
            SchemaVersion = source.SchemaVersion,
            Priority = source.Priority,
            Payload = translatedPayload,
            Metadata = new Dictionary<string, string>(source.Metadata),
        };

        await _producer.PublishAsync(translated, _options.TargetTopic, cancellationToken);

        _logger.LogDebug(
            "Message {SourceMessageId} (type={SourceType}) translated to {TargetType} " +
            "and published to '{TargetTopic}' as {TranslatedMessageId}",
            source.MessageId,
            source.MessageType,
            translated.MessageType,
            _options.TargetTopic,
            translated.MessageId);

        return new TranslationResult<TOut>(translated, source.MessageId, _options.TargetTopic);
    }
}
