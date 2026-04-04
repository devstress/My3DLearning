using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Conditionally routes messages through a validation, debug, or test pipeline
/// before normal processing. Implements the Detour Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// When the detour is enabled, messages are routed to the detour topic for
/// additional processing (e.g. validation, logging, or test capture). When
/// disabled, messages pass straight through to the normal output topic.
/// </para>
/// <para>
/// The detour condition can be evaluated per-message using the configured
/// predicate, or globally enabled/disabled via runtime control.
/// </para>
/// </remarks>
public interface IDetour
{
    /// <summary>
    /// Routes the message through the detour pipeline when the detour condition
    /// is active, or directly to the output topic when inactive.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message to route.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DetourResult"/> describing the routing decision.</returns>
    Task<DetourResult> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables the global detour switch.
    /// When enabled, all messages are routed through the detour pipeline
    /// regardless of per-message predicates.
    /// </summary>
    /// <param name="enabled">Whether to enable the detour.</param>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Returns whether the detour is currently enabled globally.
    /// </summary>
    bool IsEnabled { get; }
}
