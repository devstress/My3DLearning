using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Connector.Http;

/// <summary>
/// HTTP connector that serializes <see cref="IntegrationEnvelope{T}"/> payloads, adds
/// platform correlation headers, and optionally handles bearer-token authentication with
/// transparent caching.
/// </summary>
public sealed class HttpConnector : IHttpConnector
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string MessageIdHeader = "X-Message-Id";

    private readonly HttpClient _client;
    private readonly ITokenCache _tokenCache;
    private readonly HttpConnectorOptions _options;
    private readonly ILogger<HttpConnector> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="HttpConnector"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to create the named <c>HttpConnector</c> client.</param>
    /// <param name="tokenCache">Cache for bearer tokens.</param>
    /// <param name="options">Connector options.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="HttpConnectorOptions.BaseUrl"/> is empty.</exception>
    public HttpConnector(
        IHttpClientFactory httpClientFactory,
        ITokenCache tokenCache,
        IOptions<HttpConnectorOptions> options,
        ILogger<HttpConnector> logger)
    {
        _options = options.Value;
        _tokenCache = tokenCache;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("HttpConnectorOptions.BaseUrl must not be null or empty.");

        _client = httpClientFactory.CreateClient("HttpConnector");
    }

    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TPayload, TResponse>(
        IntegrationEnvelope<TPayload> envelope,
        string relativeUrl,
        HttpMethod method,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return await SendCoreAsync<TPayload, TResponse>(envelope, relativeUrl, method, extraHeaderName: null, extraHeaderValue: null, ct);
    }

    /// <inheritdoc />
    public async Task<TResponse> SendWithTokenAsync<TPayload, TResponse>(
        IntegrationEnvelope<TPayload> envelope,
        string relativeUrl,
        HttpMethod method,
        string tokenEndpoint,
        string tokenRequestBody,
        string tokenHeaderName,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var token = await ResolveTokenAsync(tokenEndpoint, tokenRequestBody, ct);

        string headerName;
        string headerValue;

        if (string.Equals(tokenHeaderName, "Authorization", StringComparison.OrdinalIgnoreCase))
        {
            headerName = "Authorization";
            headerValue = $"Bearer {token}";
        }
        else
        {
            headerName = tokenHeaderName;
            headerValue = token;
        }

        return await SendCoreAsync<TPayload, TResponse>(envelope, relativeUrl, method, headerName, headerValue, ct);
    }

    private async Task<TResponse> SendCoreAsync<TPayload, TResponse>(
        IntegrationEnvelope<TPayload> envelope,
        string relativeUrl,
        HttpMethod method,
        string? extraHeaderName,
        string? extraHeaderValue,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, relativeUrl);

        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, envelope.CorrelationId.ToString());
        request.Headers.TryAddWithoutValidation(MessageIdHeader, envelope.MessageId.ToString());

        if (extraHeaderName is not null)
            request.Headers.TryAddWithoutValidation(extraHeaderName, extraHeaderValue);

        if (method != HttpMethod.Get && method != HttpMethod.Delete && method != HttpMethod.Head)
        {
            request.Content = JsonContent.Create(envelope);
        }

        _logger.LogInformation(
            "Sending {Method} to {Url} with correlation {CorrelationId}",
            method, relativeUrl, envelope.CorrelationId);

        using var response = await _client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
        return result!;
    }

    private async Task<string> ResolveTokenAsync(string tokenEndpoint, string tokenRequestBody, CancellationToken ct)
    {
        if (_tokenCache.TryGetToken(tokenEndpoint, out var cached) && cached is not null)
        {
            _logger.LogDebug("Using cached token for endpoint {TokenEndpoint}", tokenEndpoint);
            return cached;
        }

        _logger.LogInformation("Fetching new token from {TokenEndpoint}", tokenEndpoint);

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new StringContent(tokenRequestBody)
        };

        using var tokenResponse = await _client.SendAsync(tokenRequest, ct);
        tokenResponse.EnsureSuccessStatusCode();

        var json = await tokenResponse.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var token = doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Token endpoint response did not contain 'access_token'.");

        _tokenCache.SetToken(tokenEndpoint, token, TimeSpan.FromSeconds(_options.CacheTokenExpirySeconds));
        return token;
    }
}
