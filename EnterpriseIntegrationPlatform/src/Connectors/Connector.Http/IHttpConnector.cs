using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Connectors.Http;

/// <summary>
/// Sends and receives messages over HTTP/HTTPS.
/// </summary>
public interface IHttpConnector
{
    /// <summary>Sends a payload to the specified URL via HTTP POST.</summary>
    Task<HttpConnectorResponse> SendAsync<T>(
        Uri endpoint,
        IntegrationEnvelope<T> envelope,
        CancellationToken ct = default);

    /// <summary>Retrieves data from the specified URL via HTTP GET.</summary>
    Task<HttpConnectorResponse> GetAsync(
        Uri endpoint,
        Dictionary<string, string>? headers = null,
        CancellationToken ct = default);
}

/// <summary>
/// Response from an HTTP connector operation.
/// </summary>
/// <param name="StatusCode">HTTP status code returned by the remote endpoint.</param>
/// <param name="Body">Response body content.</param>
/// <param name="IsSuccess">True when the status code indicates success (2xx).</param>
public record HttpConnectorResponse(
    int StatusCode,
    string? Body,
    bool IsSuccess);
