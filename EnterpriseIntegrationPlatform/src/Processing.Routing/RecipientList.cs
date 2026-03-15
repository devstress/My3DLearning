using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Recipient List — routes a single message to multiple destinations
/// determined dynamically at runtime. Equivalent to BizTalk dynamic
/// send port groups with expression-based routing.
/// </summary>
public interface IRecipientList<T>
{
    /// <summary>Determines which recipients should receive this message.</summary>
    IReadOnlyList<string> DetermineRecipients(IntegrationEnvelope<T> envelope);
}

/// <summary>
/// In-memory recipient list using predicate-based rules.
/// Each matching rule contributes its destination to the list.
/// </summary>
public sealed class RecipientList<T> : IRecipientList<T>
{
    private readonly List<(Func<IntegrationEnvelope<T>, bool> Predicate, string Recipient)> _rules = new();

    /// <summary>Adds a recipient rule. All matching rules contribute destinations.</summary>
    public RecipientList<T> AddRecipient(
        Func<IntegrationEnvelope<T>, bool> predicate, string recipient)
    {
        _rules.Add((predicate, recipient));
        return this;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> DetermineRecipients(IntegrationEnvelope<T> envelope) =>
        _rules
            .Where(r => r.Predicate(envelope))
            .Select(r => r.Recipient)
            .ToList();
}
