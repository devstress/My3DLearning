using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Content Filter — removes or normalizes fields from a message,
/// producing a simpler or canonical output. Equivalent to BizTalk
/// Map that strips unnecessary fields or a pipeline component that
/// removes sensitive data before external routing.
/// </summary>
public interface IContentFilter<TIn, TOut>
{
    /// <summary>Filters the message, removing unwanted fields.</summary>
    IntegrationEnvelope<TOut> Filter(IntegrationEnvelope<TIn> input);
}

/// <summary>
/// Delegate-based content filter.
/// </summary>
public sealed class ContentFilter<TIn, TOut> : IContentFilter<TIn, TOut>
{
    private readonly Func<TIn, TOut> _filterFunc;

    public ContentFilter(Func<TIn, TOut> filterFunc)
    {
        _filterFunc = filterFunc;
    }

    /// <inheritdoc />
    public IntegrationEnvelope<TOut> Filter(IntegrationEnvelope<TIn> input)
    {
        var filtered = _filterFunc(input.Payload);

        return IntegrationEnvelope<TOut>.Create(
            filtered,
            input.Source,
            input.MessageType,
            input.CorrelationId,
            input.MessageId);
    }
}
