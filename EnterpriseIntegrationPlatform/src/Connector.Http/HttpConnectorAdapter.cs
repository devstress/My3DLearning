using System.Text.Json;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Connector.Http;

/// <summary>
/// Adapts <see cref="IHttpConnector"/> to the unified <see cref="IConnector"/> interface,
/// enabling HTTP connectors to participate in the platform's connector registry.
/// </summary>
public sealed class HttpConnectorAdapter : IConnector
{
    private readonly IHttpConnector _httpConnector;
    private readonly HttpConnectorOptions _options;
    private readonly ILogger<HttpConnectorAdapter> _logger;

    /// <summary>Initialises a new instance of <see cref="HttpConnectorAdapter"/>.</summary>
    /// <param name="name">The unique connector name (e.g. "order-api").</param>
    /// <param name="httpConnector">The underlying HTTP connector.</param>
    /// <param name="options">HTTP connector options (used for health checks).</param>
    /// <param name="logger">Logger instance.</param>
    public HttpConnectorAdapter(
        string name,
        IHttpConnector httpConnector,
        IOptions<HttpConnectorOptions> options,
        ILogger<HttpConnectorAdapter> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(httpConnector);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        Name = name;
        _httpConnector = httpConnector;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ConnectorType ConnectorType => ConnectorType.Http;

    /// <inheritdoc />
    public async Task<ConnectorResult> SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        ConnectorSendOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(options);

        var destination = options.Destination ?? "/";
        try
        {
            var json = await _httpConnector.SendAsync<T, JsonElement>(
                envelope, destination, HttpMethod.Post, cancellationToken);

            _logger.LogInformation(
                "HTTP send to '{Destination}' succeeded for connector '{ConnectorName}'",
                destination, Name);

            return ConnectorResult.Ok(Name, $"POST {destination} succeeded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HTTP send to '{Destination}' failed for connector '{ConnectorName}'",
                destination, Name);

            return ConnectorResult.Fail(Name, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri(_options.BaseUrl),
                Timeout = TimeSpan.FromSeconds(5),
            };

            using var response = await client.GetAsync("/", cancellationToken);
            var healthy = response.IsSuccessStatusCode;

            _logger.LogDebug(
                "Health probe for HTTP connector '{ConnectorName}': {Status}",
                Name, healthy ? "Healthy" : "Unhealthy");

            return healthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Health probe for HTTP connector '{ConnectorName}' failed", Name);
            return false;
        }
    }
}
