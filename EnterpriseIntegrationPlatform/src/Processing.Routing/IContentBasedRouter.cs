using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Routes an <see cref="IntegrationEnvelope{T}"/> to the appropriate message broker
/// topic based on the message's content and configured routing rules.
/// </summary>
public interface IContentBasedRouter
{
    /// <summary>
    /// Evaluates the routing rules against <paramref name="envelope"/> and publishes it
    /// to the selected topic via the configured message broker producer.
    /// </summary>
    /// <typeparam name="T">The payload type of the envelope.</typeparam>
    /// <param name="envelope">The message envelope to route.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="RoutingDecision"/> describing the topic selected and the rule that
    /// matched (or <see langword="null"/> when the default topic was used).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="envelope"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no rule matches and no default topic is configured.
    /// </exception>
    Task<RoutingDecision> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
