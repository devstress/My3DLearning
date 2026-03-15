using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Normalizer — converts messages from multiple source formats into a
/// single canonical format. Routes each incoming format through the
/// appropriate translator. Equivalent to BizTalk Flat File Disassembler
/// or custom pipeline components that normalize EDI, XML, CSV, or JSON
/// into the canonical IntegrationEnvelope schema.
/// </summary>
public interface IMessageNormalizer<TCanonical>
{
    /// <summary>Registers a normalizer for a specific message type.</summary>
    void Register<TSource>(string messageType, Func<TSource, TCanonical> normalizer);

    /// <summary>
    /// Normalizes the raw payload (deserialized as object) based on message type.
    /// </summary>
    IntegrationEnvelope<TCanonical> Normalize(
        string messageType, object rawPayload,
        string source, Guid? correlationId = null);
}

/// <summary>
/// In-memory normalizer with registered converters per message type.
/// </summary>
public sealed class MessageNormalizer<TCanonical> : IMessageNormalizer<TCanonical>
{
    private readonly Dictionary<string, Func<object, TCanonical>> _converters = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void Register<TSource>(string messageType, Func<TSource, TCanonical> normalizer) =>
        _converters[messageType] = raw => normalizer((TSource)raw);

    /// <inheritdoc />
    public IntegrationEnvelope<TCanonical> Normalize(
        string messageType, object rawPayload,
        string source, Guid? correlationId = null)
    {
        if (!_converters.TryGetValue(messageType, out var converter))
            throw new InvalidOperationException(
                $"No normalizer registered for message type '{messageType}'.");

        var canonical = converter(rawPayload);

        return IntegrationEnvelope<TCanonical>.Create(
            canonical, source, messageType, correlationId);
    }
}
