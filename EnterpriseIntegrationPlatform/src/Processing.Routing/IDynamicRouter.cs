using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Routes an <see cref="IntegrationEnvelope{T}"/> to a destination determined at runtime
/// from a routing table maintained by downstream participants via
/// <see cref="IRouterControlChannel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Unlike the <see cref="IContentBasedRouter"/> whose rules are configured statically,
/// the Dynamic Router learns destinations at runtime. Downstream participants register
/// and unregister via control messages, and the router's routing table evolves over time.
/// </para>
/// <para>
/// This is the Enterprise Integration Patterns "Dynamic Router" pattern.
/// </para>
/// </remarks>
public interface IDynamicRouter
{
    /// <summary>
    /// Evaluates the routing table against <paramref name="envelope"/> and publishes it
    /// to the resolved destination via the configured message broker producer.
    /// </summary>
    /// <typeparam name="T">The payload type of the envelope.</typeparam>
    /// <param name="envelope">The message envelope to route.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="DynamicRoutingDecision"/> describing the destination selected and
    /// whether the fallback topic was used.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="envelope"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no routing table entry matches and no fallback topic is configured.
    /// </exception>
    Task<DynamicRoutingDecision> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
