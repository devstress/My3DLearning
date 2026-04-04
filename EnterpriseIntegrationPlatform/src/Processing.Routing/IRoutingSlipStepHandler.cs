using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Handles execution of a single named routing slip step.
/// Implementations are registered in the DI container and resolved by step name.
/// </summary>
public interface IRoutingSlipStepHandler
{
    /// <summary>
    /// The step name this handler processes.
    /// </summary>
    string StepName { get; }

    /// <summary>
    /// Executes the step logic against the given envelope.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope to process.</param>
    /// <param name="parameters">Optional step parameters from the routing slip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> on success; <c>false</c> on failure.</returns>
    Task<bool> HandleAsync<T>(
        IntegrationEnvelope<T> envelope,
        IReadOnlyDictionary<string, string>? parameters,
        CancellationToken cancellationToken = default);
}
