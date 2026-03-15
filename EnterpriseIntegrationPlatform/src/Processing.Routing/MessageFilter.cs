using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Message Filter — discards messages that do not match a set of criteria.
/// Equivalent to BizTalk Receive Port filter expressions that prevent
/// messages from entering an orchestration.
/// </summary>
public interface IMessageFilter<T>
{
    /// <summary>Returns true if the message should be processed; false to discard.</summary>
    bool Accept(IntegrationEnvelope<T> envelope);
}

/// <summary>
/// Predicate-based message filter implementation.
/// </summary>
public sealed class MessageFilter<T> : IMessageFilter<T>
{
    private readonly List<Func<IntegrationEnvelope<T>, bool>> _predicates = new();

    /// <summary>Adds a predicate that must be satisfied for the message to pass.</summary>
    public MessageFilter<T> MustSatisfy(Func<IntegrationEnvelope<T>, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    /// <inheritdoc />
    public bool Accept(IntegrationEnvelope<T> envelope) =>
        _predicates.Count == 0 || _predicates.All(p => p(envelope));
}
