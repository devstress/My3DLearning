using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Enrichment source that fetches data from an HTTP endpoint.
/// Extracts the current HTTP logic from <see cref="ContentEnricher"/>.
/// </summary>
public sealed class HttpEnrichmentSource : IEnrichmentSource
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ContentEnricherOptions _options;
    private readonly ILogger<HttpEnrichmentSource> _logger;

    /// <summary>Initialises a new instance of <see cref="HttpEnrichmentSource"/>.</summary>
    public HttpEnrichmentSource(
        IHttpClientFactory httpClientFactory,
        ContentEnricherOptions options,
        ILogger<HttpEnrichmentSource> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<JsonNode?> FetchAsync(string lookupKey, CancellationToken ct = default)
    {
        var url = _options.EndpointUrlTemplate.Replace("{key}", lookupKey, StringComparison.OrdinalIgnoreCase);

        using var client = _httpClientFactory.CreateClient("ContentEnricher");
        client.Timeout = _options.Timeout;

        _logger.LogDebug("HTTP enrichment: GET {Url}", url);

        using var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonNode.Parse(json);
    }
}
