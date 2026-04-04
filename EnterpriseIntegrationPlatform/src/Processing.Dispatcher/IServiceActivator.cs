using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Dispatcher;

/// <summary>
/// Invokes a service operation from a message and optionally publishes a reply.
/// Implements the Service Activator Enterprise Integration Pattern — connecting
/// messaging infrastructure to application services.
/// </summary>
public interface IServiceActivator
{
    /// <summary>
    /// Invokes the configured service with the given <paramref name="envelope"/>.
    /// If the service returns a result and the envelope specifies a
    /// <see cref="IntegrationEnvelope{T}.ReplyTo"/> address, the result is published
    /// to that address.
    /// </summary>
    /// <typeparam name="TRequest">Request payload type.</typeparam>
    /// <typeparam name="TResponse">Response payload type.</typeparam>
    /// <param name="envelope">The inbound message triggering the service invocation.</param>
    /// <param name="serviceOperation">
    /// The service operation to invoke. Receives the request envelope and returns
    /// a response payload, or <c>null</c> if no reply is needed.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ServiceActivatorResult"/> describing the outcome.</returns>
    Task<ServiceActivatorResult> InvokeAsync<TRequest, TResponse>(
        IntegrationEnvelope<TRequest> envelope,
        Func<IntegrationEnvelope<TRequest>, CancellationToken, Task<TResponse?>> serviceOperation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the configured service with the given <paramref name="envelope"/>
    /// without producing a reply (fire-and-forget).
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The inbound message triggering the service invocation.</param>
    /// <param name="serviceOperation">
    /// The service operation to invoke. Receives the request envelope.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ServiceActivatorResult"/> describing the outcome.</returns>
    Task<ServiceActivatorResult> InvokeAsync<T>(
        IntegrationEnvelope<T> envelope,
        Func<IntegrationEnvelope<T>, CancellationToken, Task> serviceOperation,
        CancellationToken cancellationToken = default);
}
