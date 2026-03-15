using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Content-Based Router — routes each message to exactly one destination
/// based on the first matching predicate. This is the most common BizTalk
/// routing pattern (equivalent to BizTalk filter expressions on Send Ports).
/// </summary>
public sealed class ContentBasedRouter<T> : IMessageRouter
{
    private readonly List<(Func<IntegrationEnvelope<T>, bool> Predicate, string Destination)> _routes = new();
    private string? _defaultDestination;

    /// <summary>Adds a routing rule. The first matching predicate wins.</summary>
    public ContentBasedRouter<T> When(
        Func<IntegrationEnvelope<T>, bool> predicate, string destination)
    {
        _routes.Add((predicate, destination));
        return this;
    }

    /// <summary>Sets the fallback destination when no predicate matches.</summary>
    public ContentBasedRouter<T> Otherwise(string destination)
    {
        _defaultDestination = destination;
        return this;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Route<TPayload>(IntegrationEnvelope<TPayload> envelope)
    {
        if (envelope is IntegrationEnvelope<T> typed)
        {
            foreach (var (predicate, destination) in _routes)
            {
                if (predicate(typed))
                    return new[] { destination };
            }
        }

        return _defaultDestination is not null
            ? new[] { _defaultDestination }
            : Array.Empty<string>();
    }
}
