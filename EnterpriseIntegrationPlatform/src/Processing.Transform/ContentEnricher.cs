using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Production implementation of <see cref="IContentEnricher"/>.
/// Fetches supplementary data from an HTTP endpoint and merges it into the
/// JSON payload at a configured target path.
/// </summary>
/// <remarks>
/// <para>
/// The enricher extracts a lookup key from the source payload using
/// <see cref="ContentEnricherOptions.LookupKeyPath"/>, substitutes it into the
/// endpoint URL template, and performs an HTTP GET. The response body is merged
/// at <see cref="ContentEnricherOptions.MergeTargetPath"/>.
/// </para>
/// <para>
/// Thread-safe. Designed to run as a Temporal activity.
/// </para>
/// </remarks>
public sealed class ContentEnricher : IContentEnricher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ContentEnricherOptions _options;
    private readonly ILogger<ContentEnricher> _logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Initialises a new instance of <see cref="ContentEnricher"/>.</summary>
    public ContentEnricher(
        IHttpClientFactory httpClientFactory,
        IOptions<ContentEnricherOptions> options,
        ILogger<ContentEnricher> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> EnrichAsync(
        string payload,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var sourceNode = JsonNode.Parse(payload)
            ?? throw new InvalidOperationException("Payload is not valid JSON.");

        var lookupKey = ExtractValue(sourceNode, _options.LookupKeyPath);
        if (lookupKey is null)
        {
            _logger.LogWarning(
                "Lookup key path '{Path}' not found in payload for correlation {CorrelationId}",
                _options.LookupKeyPath,
                correlationId);

            if (_options.FallbackOnFailure)
                return MergeOrReturnOriginal(sourceNode, _options.FallbackValue);

            throw new InvalidOperationException(
                $"Lookup key path '{_options.LookupKeyPath}' not found in payload.");
        }

        var url = _options.EndpointUrlTemplate.Replace("{key}", lookupKey, StringComparison.OrdinalIgnoreCase);

        try
        {
            using var client = _httpClientFactory.CreateClient("ContentEnricher");
            client.Timeout = _options.Timeout;

            _logger.LogDebug(
                "Enriching payload for correlation {CorrelationId} via {Url}",
                correlationId,
                url);

            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var enrichmentJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var enrichmentNode = JsonNode.Parse(enrichmentJson);

            if (enrichmentNode is null)
            {
                _logger.LogWarning(
                    "Enrichment response was null/empty for correlation {CorrelationId}",
                    correlationId);
                return MergeOrReturnOriginal(sourceNode, _options.FallbackValue);
            }

            SetValue(sourceNode, _options.MergeTargetPath, enrichmentNode);

            _logger.LogDebug(
                "Enrichment merged at '{TargetPath}' for correlation {CorrelationId}",
                _options.MergeTargetPath,
                correlationId);

            return sourceNode.ToJsonString(s_jsonOptions);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (_options.FallbackOnFailure)
        {
            _logger.LogWarning(
                ex,
                "Enrichment call failed for correlation {CorrelationId}; using fallback",
                correlationId);

            return MergeOrReturnOriginal(sourceNode, _options.FallbackValue);
        }
    }

    /// <summary>
    /// Extracts a scalar string value from the JSON node at the given dot-separated path.
    /// </summary>
    internal static string? ExtractValue(JsonNode root, string path)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        JsonNode? current = root;

        foreach (var segment in segments)
        {
            if (current is not JsonObject obj || !obj.TryGetPropertyValue(segment, out current))
                return null;
        }

        return current?.GetValue<JsonElement>().ValueKind switch
        {
            JsonValueKind.String => current.GetValue<string>(),
            JsonValueKind.Number => current.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => current?.ToJsonString(),
        };
    }

    /// <summary>
    /// Sets a value in the JSON tree at the given dot-separated path, creating
    /// intermediate objects as needed.
    /// </summary>
    internal static void SetValue(JsonNode root, string path, JsonNode value)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            throw new ArgumentException("Target path must not be empty.", nameof(path));

        var current = root as JsonObject
            ?? throw new InvalidOperationException("Root node must be a JSON object.");

        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (!current.TryGetPropertyValue(segments[i], out var child) || child is not JsonObject childObj)
            {
                childObj = new JsonObject();
                current[segments[i]] = childObj;
            }
            current = childObj;
        }

        current[segments[^1]] = value;
    }

    private string MergeOrReturnOriginal(JsonNode source, string? fallbackJson)
    {
        if (fallbackJson is not null)
        {
            var fallbackNode = JsonNode.Parse(fallbackJson);
            if (fallbackNode is not null)
            {
                SetValue(source, _options.MergeTargetPath, fallbackNode);
                return source.ToJsonString(s_jsonOptions);
            }
        }

        return source.ToJsonString(s_jsonOptions);
    }
}
