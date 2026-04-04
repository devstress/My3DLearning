using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Publishes control messages to the platform's control channel for runtime
/// configuration changes. Implements the Control Bus Enterprise Integration
/// Pattern — enabling administrative operations to flow through the same
/// messaging infrastructure as business messages.
/// </summary>
public interface IControlBus
{
    /// <summary>
    /// Publishes a control command to the control channel.
    /// Subscribers (platform services) receive and apply the configuration change.
    /// </summary>
    /// <typeparam name="T">The payload type of the control command.</typeparam>
    /// <param name="command">The control command payload.</param>
    /// <param name="commandType">Logical type name of the control command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ControlBusResult"/> describing the outcome.</returns>
    Task<ControlBusResult> PublishCommandAsync<T>(
        T command,
        string commandType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes a handler to the control channel for a specific command type.
    /// </summary>
    /// <typeparam name="T">The payload type of the control command.</typeparam>
    /// <param name="commandType">The command type to listen for.</param>
    /// <param name="handler">The handler invoked when a matching command arrives.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SubscribeAsync<T>(
        string commandType,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default);
}
