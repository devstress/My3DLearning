using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Message Translator — converts a message from one format to another
/// using a delegate-based mapping function.
/// Equivalent to BizTalk Map (XSLT / Functoid-based transformation).
/// </summary>
public sealed class MessageTranslator<TIn, TOut> : IMessageTransformer<TIn, TOut>
{
    private readonly Func<TIn, TOut> _map;
    private readonly string? _outputMessageType;

    /// <param name="map">The mapping function from input to output payload.</param>
    /// <param name="outputMessageType">Optional override for the message type name.</param>
    public MessageTranslator(Func<TIn, TOut> map, string? outputMessageType = null)
    {
        _map = map;
        _outputMessageType = outputMessageType;
    }

    /// <inheritdoc />
    public IntegrationEnvelope<TOut> Transform(IntegrationEnvelope<TIn> input)
    {
        var mapped = _map(input.Payload);

        return IntegrationEnvelope<TOut>.Create(
            mapped,
            input.Source,
            _outputMessageType ?? input.MessageType,
            input.CorrelationId,
            input.MessageId);
    }
}
