using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Resolves a list of target destinations for a message and publishes it to ALL
/// resolved recipients (fan-out). This is the Enterprise Integration Patterns
/// "Recipient List" pattern.
/// </summary>
/// <remarks>
/// <para>
/// Distinct from Content-Based Router (single route) and Scatter-Gather (expects replies).
/// The Recipient List publishes the same unmodified message to every resolved destination.
/// </para>
/// </remarks>
public interface IRecipientList
{
    /// <summary>
    /// Resolves the list of destinations for <paramref name="envelope"/> and publishes
    /// it to all resolved destinations.
    /// </summary>
    /// <typeparam name="T">The payload type of the envelope.</typeparam>
    /// <param name="envelope">The message envelope to route.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="RecipientListResult"/> describing which destinations received the message.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="envelope"/> is <see langword="null"/>.
    /// </exception>
    Task<RecipientListResult> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
