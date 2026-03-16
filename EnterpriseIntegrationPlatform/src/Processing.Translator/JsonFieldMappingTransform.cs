using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Translator;

/// <summary>
/// A <see cref="IPayloadTransform{TIn,TOut}"/> that maps fields from a source
/// <see cref="JsonElement"/> to a new <see cref="JsonElement"/> using the
/// <see cref="FieldMapping"/> list defined in <see cref="TranslatorOptions.FieldMappings"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each mapping reads a value from <see cref="FieldMapping.SourcePath"/> (dot-separated)
/// in the source document, or uses <see cref="FieldMapping.StaticValue"/> when set, and
/// writes it to <see cref="FieldMapping.TargetPath"/> (dot-separated) in the target
/// document. Intermediate JSON objects along <see cref="FieldMapping.TargetPath"/> are
/// created automatically. When a source path segment is missing, that mapping is silently
/// skipped — the key is omitted from the target document.
/// </para>
/// <para>
/// All extracted values are represented as strings in the target document. This matches
/// the canonical string representation used by other field-inspection components in the
/// platform (e.g. the Content-Based Router).
/// </para>
/// </remarks>
public sealed class JsonFieldMappingTransform : IPayloadTransform<JsonElement, JsonElement>
{
    private readonly IReadOnlyList<FieldMapping> _mappings;

    /// <summary>
    /// Initialises a new instance of <see cref="JsonFieldMappingTransform"/> using the
    /// field mappings from the supplied <paramref name="options"/>.
    /// </summary>
    public JsonFieldMappingTransform(IOptions<TranslatorOptions> options)
    {
        _mappings = options.Value.FieldMappings;
    }

    /// <inheritdoc />
    public JsonElement Transform(JsonElement source)
    {
        var target = new JsonObject();

        foreach (var mapping in _mappings)
        {
            var value = mapping.StaticValue ?? ExtractValue(source, mapping.SourcePath);
            if (value is not null)
                SetValue(target, mapping.TargetPath, value);
        }

        return JsonSerializer.SerializeToElement(target);
    }

    /// <summary>
    /// Navigates <paramref name="root"/> along the dot-separated <paramref name="path"/>
    /// and returns the string representation of the terminal value, or
    /// <see langword="null"/> when any segment is absent or the value is JSON null.
    /// </summary>
    private static string? ExtractValue(JsonElement root, string path)
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

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => current.GetRawText(),
        };
    }

    /// <summary>
    /// Writes <paramref name="value"/> into <paramref name="target"/> at the
    /// dot-separated <paramref name="path"/>, creating intermediate
    /// <see cref="JsonObject"/> nodes as needed.
    /// </summary>
    private static void SetValue(JsonObject target, string path, string value)
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

        current[segments[^1]] = JsonValue.Create(value);
    }
}
