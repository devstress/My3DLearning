using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Gateway.Api;

/// <summary>
/// HTTP-based implementation of <see cref="IMessagingGateway"/>.
/// Encapsulates all messaging access behind a simple API, forwarding requests
/// to downstream services via HTTP. Implements the Messaging Gateway EIP pattern.
/// Thread-safe and designed for use as a singleton or scoped service.
/// </summary>
public sealed class HttpMessagingGateway : IMessagingGateway
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpMessagingGateway> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Initializes a new instance of <see cref="HttpMessagingGateway"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="serializerOptions">
    /// Optional JSON serializer options. When <c>null</c>, default web-friendly options are used.
    /// </param>
    public HttpMessagingGateway(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpMessagingGateway> logger,
        JsonSerializerOptions? serializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    /// <inheritdoc />
    public async Task<GatewayResponse> SendAsync<T>(
        string destination,
        T payload,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(payload);

        var correlationId = Guid.NewGuid();
        var client = _httpClientFactory.CreateClient("downstream");

        using var request = new HttpRequestMessage(HttpMethod.Post, destination);
        request.Content = JsonContent.Create(payload, options: _serializerOptions);
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId.ToString("D"));

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        _logger.LogDebug(
            "Gateway sending message to {Destination} with correlation {CorrelationId}",
            destination,
            correlationId);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);

            var success = response.IsSuccessStatusCode;

            if (!success)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Gateway received non-success status {StatusCode} from {Destination}: {Error}",
                    (int)response.StatusCode,
                    destination,
                    errorBody);

                return new GatewayResponse
                {
                    CorrelationId = correlationId,
                    Success = false,
                    StatusCode = (int)response.StatusCode,
                    Error = errorBody,
                };
            }

            _logger.LogDebug(
                "Gateway message sent successfully to {Destination} with correlation {CorrelationId}",
                destination,
                correlationId);

            return new GatewayResponse
            {
                CorrelationId = correlationId,
                Success = true,
                StatusCode = (int)response.StatusCode,
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Gateway failed to send to {Destination}: {Error}",
                destination,
                ex.Message);

            return new GatewayResponse
            {
                CorrelationId = correlationId,
                Success = false,
                StatusCode = 502,
                Error = $"Downstream unavailable: {ex.Message}",
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex,
                "Gateway timed out sending to {Destination}",
                destination);

            return new GatewayResponse
            {
                CorrelationId = correlationId,
                Success = false,
                StatusCode = 504,
                Error = "Gateway timeout",
            };
        }
    }

    /// <inheritdoc />
    public async Task<GatewayResponse<TResponse>> SendAndReceiveAsync<TRequest, TResponse>(
        string destination,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(request);

        var correlationId = Guid.NewGuid();
        var effectiveTimeout = timeout ?? DefaultTimeout;
        var client = _httpClientFactory.CreateClient("downstream");
        client.Timeout = effectiveTimeout;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, destination);
        httpRequest.Content = JsonContent.Create(request, options: _serializerOptions);
        httpRequest.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId.ToString("D"));

        _logger.LogDebug(
            "Gateway sending request-reply to {Destination} with correlation {CorrelationId}, timeout {Timeout}",
            destination,
            correlationId,
            effectiveTimeout);

        try
        {
            using var response = await client.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Gateway request-reply received {StatusCode} from {Destination}: {Error}",
                    (int)response.StatusCode,
                    destination,
                    errorBody);

                return new GatewayResponse<TResponse>
                {
                    CorrelationId = correlationId,
                    Success = false,
                    StatusCode = (int)response.StatusCode,
                    Error = errorBody,
                };
            }

            var responsePayload = await response.Content.ReadFromJsonAsync<TResponse>(
                _serializerOptions,
                cancellationToken);

            _logger.LogDebug(
                "Gateway request-reply succeeded for {Destination} with correlation {CorrelationId}",
                destination,
                correlationId);

            return new GatewayResponse<TResponse>
            {
                CorrelationId = correlationId,
                Success = true,
                StatusCode = (int)response.StatusCode,
                Payload = responsePayload,
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Gateway request-reply failed for {Destination}: {Error}",
                destination,
                ex.Message);

            return new GatewayResponse<TResponse>
            {
                CorrelationId = correlationId,
                Success = false,
                StatusCode = 502,
                Error = $"Downstream unavailable: {ex.Message}",
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex,
                "Gateway request-reply timed out for {Destination}",
                destination);

            return new GatewayResponse<TResponse>
            {
                CorrelationId = correlationId,
                Success = false,
                StatusCode = 504,
                Error = "Gateway timeout",
            };
        }
    }
}
