using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Connector.Http;

/// <summary>
/// Sends <see cref="IntegrationEnvelope{T}"/> payloads over HTTP and deserializes responses.
/// </summary>
public interface IHttpConnector
{
    /// <summary>
    /// Sends a message envelope to the specified relative URL using the given HTTP method.
    /// </summary>
    /// <typeparam name="TPayload">Type of the envelope payload.</typeparam>
    /// <typeparam name="TResponse">Expected response type.</typeparam>
    /// <param name="envelope">The message envelope to send.</param>
    /// <param name="relativeUrl">URL path relative to the configured base URL.</param>
    /// <param name="method">HTTP method to use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deserialized response of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> SendAsync<TPayload, TResponse>(
        IntegrationEnvelope<TPayload> envelope,
        string relativeUrl,
        HttpMethod method,
        CancellationToken ct);

    /// <summary>
    /// Sends a message envelope after obtaining (and caching) a bearer token from the given
    /// token endpoint. Re-uses the cached token on subsequent calls until expiry.
    /// </summary>
    /// <typeparam name="TPayload">Type of the envelope payload.</typeparam>
    /// <typeparam name="TResponse">Expected response type.</typeparam>
    /// <param name="envelope">The message envelope to send.</param>
    /// <param name="relativeUrl">URL path relative to the configured base URL.</param>
    /// <param name="method">HTTP method to use.</param>
    /// <param name="tokenEndpoint">Absolute URL of the token endpoint.</param>
    /// <param name="tokenRequestBody">Request body sent to the token endpoint (e.g. form-encoded credentials).</param>
    /// <param name="tokenHeaderName">
    /// Header name used to attach the token. Use <c>"Authorization"</c> to send as
    /// <c>Bearer &lt;token&gt;</c>; any other value sends the raw token as the header value.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deserialized response of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> SendWithTokenAsync<TPayload, TResponse>(
        IntegrationEnvelope<TPayload> envelope,
        string relativeUrl,
        HttpMethod method,
        string tokenEndpoint,
        string tokenRequestBody,
        string tokenHeaderName,
        CancellationToken ct);
}
