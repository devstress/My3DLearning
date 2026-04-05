using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Production implementation of <see cref="INormalizer"/>.
/// Detects the incoming payload format (JSON, XML, CSV) and converts it to canonical
/// JSON. Already-JSON payloads pass through unchanged.
/// </summary>
/// <remarks>
/// <para>
/// The Normalizer pattern ensures that downstream pipeline steps only ever see a
/// single canonical format — JSON. The canonical data model for this platform is
/// <c>IntegrationEnvelope&lt;T&gt;</c>; the normalizer handles the payload
/// conversion that precedes envelope wrapping.
/// </para>
/// <para>
/// Thread-safe. Designed to run as a Temporal activity.
/// </para>
/// </remarks>
public sealed class MessageNormalizer : INormalizer
{
    private readonly NormalizerOptions _options;
    private readonly ILogger<MessageNormalizer> _logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Initialises a new instance of <see cref="MessageNormalizer"/>.</summary>
    public MessageNormalizer(
        IOptions<NormalizerOptions> options,
        ILogger<MessageNormalizer> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<NormalizationResult> NormalizeAsync(
        string payload,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        cancellationToken.ThrowIfCancellationRequested();

        var normalizedContentType = contentType.Trim().ToLowerInvariant();

        var format = DetectFormat(payload, normalizedContentType);

        _logger.LogDebug("Detected format '{Format}' for content type '{ContentType}'", format, contentType);

        var result = format switch
        {
            "JSON" => NormalizeJson(payload, contentType),
            "XML" => NormalizeXml(payload, contentType),
            "CSV" => NormalizeCsv(payload, contentType),
            _ => throw new InvalidOperationException(
                $"Unsupported format '{format}' for content type '{contentType}'. " +
                "Supported formats: JSON, XML, CSV."),
        };

        _logger.LogDebug(
            "Normalization complete: {Format} → JSON, transformed={Transformed}",
            format,
            result.WasTransformed);

        return Task.FromResult(result);
    }

    /// <summary>
    /// Detects the payload format from the content type and/or payload inspection.
    /// </summary>
    internal string DetectFormat(string payload, string normalizedContentType)
    {
        // Match by content type first.
        if (normalizedContentType.Contains("json", StringComparison.Ordinal))
            return "JSON";

        if (normalizedContentType.Contains("xml", StringComparison.Ordinal))
            return "XML";

        if (normalizedContentType.Contains("csv", StringComparison.Ordinal))
            return "CSV";

        if (_options.StrictContentType)
            throw new InvalidOperationException(
                $"Unknown content type '{normalizedContentType}'. " +
                "Set NormalizerOptions.StrictContentType to false for best-effort detection.");

        // Best-effort detection by inspecting payload content.
        var trimmed = payload.TrimStart();

        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            return "JSON";

        if (trimmed.StartsWith('<'))
            return "XML";

        // Fallback: assume CSV if it contains commas and newlines.
        if (trimmed.Contains(_options.CsvDelimiter) && trimmed.Contains('\n'))
            return "CSV";

        throw new InvalidOperationException(
            "Unable to detect payload format from content or content type. " +
            "Supported formats: JSON, XML, CSV.");
    }

    private static NormalizationResult NormalizeJson(string payload, string originalContentType)
    {
        // Validate that the payload is valid JSON.
        using var doc = JsonDocument.Parse(payload);

        // Re-serialize to ensure consistent formatting.
        var canonical = JsonSerializer.Serialize(doc.RootElement, s_jsonOptions);

        return new NormalizationResult(canonical, originalContentType, "JSON", WasTransformed: false);
    }

    private NormalizationResult NormalizeXml(string payload, string originalContentType)
    {
        var xDoc = XDocument.Parse(payload);
        if (xDoc.Root is null)
            throw new InvalidOperationException("XML document has no root element.");

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            WriteXmlElement(writer, xDoc.Root);
            writer.WriteEndObject();
        }

        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        return new NormalizationResult(json, originalContentType, "XML", WasTransformed: true);
    }

    private NormalizationResult NormalizeCsv(string payload, string originalContentType)
    {
        var lines = payload.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            throw new InvalidOperationException("CSV payload is empty.");

        var delimiter = _options.CsvDelimiter;
        var array = new JsonArray();

        if (_options.CsvHasHeaders && lines.Length > 1)
        {
            var headers = ParseCsvLine(lines[0], delimiter);

            for (var i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i], delimiter);
                var obj = new JsonObject();

                for (var j = 0; j < headers.Length; j++)
                {
                    var header = headers[j].Trim();
                    var value = j < values.Length ? values[j].Trim() : "";
                    obj[header] = value;
                }

                array.Add(obj);
            }
        }
        else
        {
            foreach (var line in lines)
            {
                var values = ParseCsvLine(line, delimiter);
                var row = new JsonArray();
                foreach (var val in values)
                    row.Add(val.Trim());
                array.Add(row);
            }
        }

        var wrapper = new JsonObject { [_options.XmlRootName] = array };
        var json = wrapper.ToJsonString(s_jsonOptions);

        return new NormalizationResult(json, originalContentType, "CSV", WasTransformed: true);
    }

    /// <summary>
    /// Parses a single CSV line, handling quoted fields with embedded delimiters.
    /// </summary>
    internal static string[] ParseCsvLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote.
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else if (c != '\r')
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }

    /// <summary>
    /// Recursively writes an XML element's children and attributes to a JSON writer.
    /// Repeated sibling elements with the same name produce JSON arrays.
    /// </summary>
    private static void WriteXmlElement(Utf8JsonWriter writer, XElement element)
    {
        var childGroups = element.Elements()
            .GroupBy(e => e.Name.LocalName)
            .ToList();

        var hasAttributes = element.Attributes().Any(a => !a.IsNamespaceDeclaration);
        var hasChildElements = childGroups.Count > 0;
        var textValue = hasChildElements ? null : element.Value;

        foreach (var attr in element.Attributes())
        {
            if (attr.IsNamespaceDeclaration)
                continue;
            writer.WriteString($"@{attr.Name.LocalName}", attr.Value);
        }

        if (hasAttributes && !hasChildElements && !string.IsNullOrEmpty(textValue))
        {
            writer.WriteString("#text", textValue);
            return;
        }

        foreach (var group in childGroups)
        {
            var items = group.ToList();

            if (items.Count > 1)
            {
                writer.WriteStartArray(group.Key);
                foreach (var child in items)
                    WriteChildValue(writer, child);
                writer.WriteEndArray();
            }
            else
            {
                writer.WritePropertyName(group.Key);
                WriteChildValue(writer, items[0]);
            }
        }
    }

    private static void WriteChildValue(Utf8JsonWriter writer, XElement child)
    {
        var hasChildren = child.HasElements || child.Attributes().Any(a => !a.IsNamespaceDeclaration);

        if (hasChildren)
        {
            writer.WriteStartObject();
            WriteXmlElement(writer, child);
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStringValue(child.Value);
        }
    }
}
