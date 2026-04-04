using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Production implementation of <see cref="IContentFilter"/>.
/// Removes all fields from a JSON payload except those matching the specified paths.
/// This is the EIP Content Filter pattern — the inverse of content enrichment.
/// </summary>
/// <remarks>
/// <para>
/// The filter operates on JSON objects. Each keep-path is a dot-separated property
/// path (e.g. <c>order.id</c>, <c>customer.address.city</c>). The filter produces a
/// new JSON object containing only the matched paths. Missing paths are silently
/// skipped.
/// </para>
/// <para>
/// Thread-safe. Designed to run as a Temporal activity.
/// </para>
/// </remarks>
public sealed class ContentFilter : IContentFilter
{
    private readonly ILogger<ContentFilter> _logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Initialises a new instance of <see cref="ContentFilter"/>.</summary>
    public ContentFilter(ILogger<ContentFilter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> FilterAsync(
        string payload,
        IReadOnlyList<string> keepPaths,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(keepPaths);

        if (keepPaths.Count == 0)
            throw new ArgumentException("At least one keep-path must be specified.", nameof(keepPaths));

        cancellationToken.ThrowIfCancellationRequested();

        using var doc = JsonDocument.Parse(payload);

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Content filter requires a JSON object payload.");

        var output = new JsonObject();

        foreach (var path in keepPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var value = ExtractValue(doc.RootElement, path);
            if (value is not null)
            {
                SetValue(output, path, value.Value);
                _logger.LogDebug("Retained path '{Path}'", path);
            }
            else
            {
                _logger.LogDebug("Path '{Path}' not found in payload; skipping", path);
            }
        }

        var result = output.ToJsonString(s_jsonOptions);

        _logger.LogDebug(
            "Content filter retained {Count} path(s); output size {Size} bytes",
            keepPaths.Count,
            result.Length);

        return Task.FromResult(result);
    }

    /// <summary>
    /// Navigates the JSON tree along a dot-separated path and returns the terminal
    /// <see cref="JsonElement"/>, or <see langword="null"/> if any segment is missing.
    /// </summary>
    internal static JsonElement? ExtractValue(JsonElement root, string path)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var current = root;

        foreach (var segment in segments)
        {
            if (current.ValueKind != JsonValueKind.Object)
                return null;

            if (!current.TryGetProperty(segment, out current))
                return null;
        }

        return current;
    }

    /// <summary>
    /// Writes a <see cref="JsonElement"/> value into the output <see cref="JsonObject"/>
    /// at the specified dot-separated path, creating intermediate objects as needed.
    /// </summary>
    internal static void SetValue(JsonObject target, string path, JsonElement value)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var current = target;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (current[segments[i]] is not JsonObject nested)
            {
                nested = new JsonObject();
                current[segments[i]] = nested;
            }

            current = nested;
        }

        current[segments[^1]] = JsonNode.Parse(value.GetRawText());
    }
}
