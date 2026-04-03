using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Evaluates a predicate against an <see cref="IntegrationEnvelope{T}"/> and either
/// passes it through to a destination or discards it (with optional DLQ routing).
/// This is the Enterprise Integration Patterns "Message Filter" pattern.
/// </summary>
public interface IMessageFilter
{
    /// <summary>
    /// Evaluates the configured predicate(s) against <paramref name="envelope"/>.
    /// If the predicate passes, the message is published to the configured output topic.
    /// If the predicate fails, the message is either silently discarded or routed to the
    /// configured discard destination (DLQ).
    /// </summary>
    /// <typeparam name="T">The payload type of the envelope.</typeparam>
    /// <param name="envelope">The message envelope to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="MessageFilterResult"/> describing whether the message passed or was discarded.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="envelope"/> is <see langword="null"/>.
    /// </exception>
    Task<MessageFilterResult> FilterAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
