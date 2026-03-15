using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Production implementation of the Message Translator EIP pattern.
/// </summary>
/// <remarks>
/// Delegates payload conversion to the supplied <see cref="IPayloadConverter{TIn,TOut}"/>
/// and preserves the full message lineage by copying <see cref="IntegrationEnvelope{T}.CorrelationId"/>,
/// <see cref="IntegrationEnvelope{T}.Source"/>, <see cref="IntegrationEnvelope{T}.Metadata"/>, and
/// <see cref="IntegrationEnvelope{T}.Priority"/> from the source envelope. A new
/// <see cref="IntegrationEnvelope{T}.MessageId"/> is generated and
/// <see cref="IntegrationEnvelope{T}.CausationId"/> is set to the source message ID.
/// </remarks>
public sealed class MessageTranslator : IMessageTranslator
{
    private readonly ILogger<MessageTranslator> _logger;

    /// <summary>Initialises a new instance of <see cref="MessageTranslator"/>.</summary>
    public MessageTranslator(ILogger<MessageTranslator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IntegrationEnvelope<TOut>> TranslateAsync<TIn, TOut>(
        IntegrationEnvelope<TIn> envelope,
        IPayloadConverter<TIn, TOut> converter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(converter);

        _logger.LogDebug(
            "Translating message {MessageId} (type={MessageType}, source={Source}) using {Converter}",
            envelope.MessageId, envelope.MessageType, envelope.Source, converter.GetType().Name);

        var convertedPayload = await converter.ConvertAsync(envelope.Payload, cancellationToken);

        var translated = new IntegrationEnvelope<TOut>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = envelope.CorrelationId,
            CausationId = envelope.MessageId,
            Timestamp = DateTimeOffset.UtcNow,
            Source = envelope.Source,
            MessageType = envelope.MessageType,
            SchemaVersion = envelope.SchemaVersion,
            Priority = envelope.Priority,
            Payload = convertedPayload,
            Metadata = envelope.Metadata,
        };

        _logger.LogDebug(
            "Translated message {SourceMessageId} → {TranslatedMessageId} (correlation={CorrelationId})",
            envelope.MessageId, translated.MessageId, translated.CorrelationId);

        return translated;
    }
}
