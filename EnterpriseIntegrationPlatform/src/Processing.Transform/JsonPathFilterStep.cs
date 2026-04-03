using System.Text.Json;
using System.Text.Json.Nodes;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Transform step that extracts a subset of a JSON payload using a simplified
/// JSONPath-like dot-notation filter. The step selects one or more properties from the
/// source JSON and produces a new JSON object containing only those properties.
/// </summary>
/// <remarks>
/// <para>
/// Each path is a dot-separated property path (e.g. <c>order.id</c>,
/// <c>customer.address.city</c>). The step reads each path from the source JSON and
/// writes the value at the same path in the output document. Missing paths are silently
/// skipped.
/// </para>
/// <para>
/// This step operates on <c>application/json</c> content only and preserves the content
/// type.
/// </para>
/// </remarks>
public sealed class JsonPathFilterStep : ITransformStep
{
    private readonly IReadOnlyList<string> _paths;

    /// <summary>
    /// Initialises a new <see cref="JsonPathFilterStep"/> with the property paths to
    /// retain.
    /// </summary>
    /// <param name="paths">
    /// One or more dot-separated property paths to extract from the source JSON.
    /// </param>
    public JsonPathFilterStep(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var list = paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if (list.Count == 0)
            throw new ArgumentException("At least one path must be specified.", nameof(paths));
        _paths = list.AsReadOnly();
    }

    /// <inheritdoc />
    public string Name => "JsonPathFilter";

    /// <inheritdoc />
    public Task<TransformContext> ExecuteAsync(
        TransformContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var doc = JsonDocument.Parse(context.Payload);
        var output = new JsonObject();

        foreach (var path in _paths)
        {
            var value = ExtractValue(doc.RootElement, path);
            if (value is not null)
                SetValue(output, path, value.Value);
        }

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
        var result = context.WithPayload(json, "application/json");
        result.Metadata[$"Step.{Name}.Applied"] = "true";
        return Task.FromResult(result);
    }

    /// <summary>
    /// Navigates the JSON tree along a dot-separated path and returns the terminal
    /// <see cref="JsonElement"/>, or <see langword="null"/> if any segment is missing.
    /// </summary>
    private static JsonElement? ExtractValue(JsonElement root, string path)
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

        return current.ValueKind == JsonValueKind.Null ? null : current;
    }

    /// <summary>
    /// Writes a <see cref="JsonElement"/> value into the output <see cref="JsonObject"/>
    /// at the specified dot-separated path, creating intermediate objects as needed.
    /// </summary>
    private static void SetValue(JsonObject target, string path, JsonElement value)
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
