namespace EnterpriseIntegrationPlatform.Gateway.Api;

/// <summary>
/// Messaging Gateway — encapsulates all access to the messaging system behind a
/// simple, domain-oriented API. The Messaging Gateway hides the messaging-specific code
/// from the rest of the application, providing a clean interface that decouples business
/// logic from messaging infrastructure.
///
/// <para>
/// In this platform, <c>Gateway.Api</c> IS the Messaging Gateway. It:
/// <list type="bullet">
/// <item>Accepts HTTP requests from external clients and internal services.</item>
/// <item>Forwards requests to downstream services (Admin.Api, OpenClaw.Web) via reverse proxy.</item>
/// <item>Applies rate limiting, authentication, CORS, and correlation tracking.</item>
/// <item>Shields clients from broker topology (Kafka, NATS, Pulsar) and transport details.</item>
/// </list>
/// </para>
///
/// <para>
/// EIP Pattern: <c>Messaging Gateway</c> (Chapter 10, p. 468 of Enterprise Integration Patterns).
/// </para>
/// </summary>
public interface IMessagingGateway
{
    /// <summary>
    /// Sends a message through the gateway to the specified destination.
    /// The gateway encapsulates all messaging infrastructure details.
    /// </summary>
    /// <typeparam name="T">The type of the message payload.</typeparam>
    /// <param name="destination">The logical destination (e.g., topic, queue, or route path).</param>
    /// <param name="payload">The message payload to send.</param>
    /// <param name="headers">Optional HTTP headers to forward.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The gateway response including status and correlation ID.</returns>
    Task<GatewayResponse> SendAsync<T>(
        string destination,
        T payload,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request through the gateway and awaits a reply (request-reply pattern).
    /// The gateway encapsulates correlation, timeout, and routing.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request payload.</typeparam>
    /// <typeparam name="TResponse">The type of the expected response payload.</typeparam>
    /// <param name="destination">The logical destination.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="timeout">Maximum time to wait for a reply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the downstream service.</returns>
    Task<GatewayResponse<TResponse>> SendAndReceiveAsync<TRequest, TResponse>(
        string destination,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
