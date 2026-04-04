using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Processes an <see cref="IntegrationEnvelope{T}"/> according to its attached
/// <see cref="RoutingSlip"/>, executing the current step and advancing the slip.
/// This is the Enterprise Integration Patterns "Routing Slip" pattern.
/// </summary>
public interface IRoutingSlipRouter
{
    /// <summary>
    /// Reads the <see cref="RoutingSlip"/> from the envelope's metadata, executes the
    /// current step via the registered <see cref="IRoutingSlipStepHandler"/>, advances
    /// the slip, and either forwards the message to a destination topic or returns
    /// control for in-process continuation.
    /// </summary>
    /// <typeparam name="T">The payload type of the envelope.</typeparam>
    /// <param name="envelope">The message envelope carrying a routing slip in its metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of executing the current step.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the envelope does not contain a routing slip or the slip is empty.
    /// </exception>
    Task<RoutingSlipStepResult> ExecuteCurrentStepAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
