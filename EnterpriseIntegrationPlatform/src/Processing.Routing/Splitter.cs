using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Splitter — breaks a composite message into individual messages.
/// Equivalent to BizTalk Debatching / Disassemble pipeline component
/// that splits batched EDI, XML, or flat-file documents into individual messages.
/// </summary>
public interface IMessageSplitter<TComposite, TItem>
{
    /// <summary>
    /// Splits a composite message into individual item envelopes.
    /// Each item inherits the correlation ID from the parent.
    /// </summary>
    IReadOnlyList<IntegrationEnvelope<TItem>> Split(
        IntegrationEnvelope<TComposite> composite);
}

/// <summary>
/// Delegate-based splitter implementation.
/// </summary>
public sealed class MessageSplitter<TComposite, TItem> : IMessageSplitter<TComposite, TItem>
{
    private readonly Func<TComposite, IEnumerable<TItem>> _splitFunc;

    public MessageSplitter(Func<TComposite, IEnumerable<TItem>> splitFunc)
    {
        _splitFunc = splitFunc;
    }

    /// <inheritdoc />
    public IReadOnlyList<IntegrationEnvelope<TItem>> Split(
        IntegrationEnvelope<TComposite> composite)
    {
        return _splitFunc(composite.Payload)
            .Select(item => IntegrationEnvelope<TItem>.Create(
                item,
                composite.Source,
                composite.MessageType + ".Item",
                composite.CorrelationId,
                composite.MessageId))
            .ToList();
    }
}
