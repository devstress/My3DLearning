using System.Text.Json;
using System.Xml.Linq;

namespace EnterpriseIntegrationPlatform.Processing.Transform.Converters;

/// <summary>
/// Converts an XML string payload to a <see cref="JsonElement"/>.
/// </summary>
/// <remarks>
/// <para>
/// XML elements without child elements are converted to JSON strings.
/// XML elements with child elements of the same name are converted to JSON arrays.
/// XML elements with mixed child element names are converted to JSON objects.
/// The root XML element is unwrapped; its children become the top-level JSON properties.
/// </para>
/// <para>
/// Attributes on the root element and child elements are included in the JSON output
/// prefixed with <c>@</c> (e.g. an attribute <c>nil="true"</c> appears as
/// <c>"@nil": "true"</c>).
/// </para>
/// </remarks>
public sealed class XmlToJsonConverter : IPayloadConverter<string, JsonElement>
{
    /// <inheritdoc />
    public Task<JsonElement> ConvertAsync(string input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        XDocument document;
        try
        {
            document = XDocument.Parse(input);
        }
        catch (Exception ex) when (ex is System.Xml.XmlException)
        {
            throw new FormatException($"Input is not valid XML: {ex.Message}", ex);
        }

        if (document.Root is null)
            throw new FormatException("Input XML has no root element.");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        WriteElement(writer, document.Root);
        writer.Flush();

        stream.Position = 0;
        return Task.FromResult(JsonDocument.Parse(stream).RootElement.Clone());
    }

    private static void WriteElement(Utf8JsonWriter writer, XElement element)
    {
        var children = element.Elements().ToList();
        var attributes = element.Attributes().Where(a => !a.IsNamespaceDeclaration).ToList();

        if (children.Count == 0 && attributes.Count == 0)
        {
            // Leaf node — emit a JSON string.
            writer.WriteStringValue(element.Value);
            return;
        }

        writer.WriteStartObject();

        // Attributes (prefixed with @).
        foreach (var attr in attributes)
        {
            writer.WriteString("@" + attr.Name.LocalName, attr.Value);
        }

        if (children.Count > 0)
        {
            // Group children by local name to detect arrays.
            var groups = children
                .GroupBy(c => c.Name.LocalName)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in groups)
            {
                writer.WritePropertyName(group.Key);

                if (group.Value.Count > 1)
                {
                    // Multiple siblings with the same name → JSON array.
                    writer.WriteStartArray();
                    foreach (var child in group.Value)
                        WriteElement(writer, child);
                    writer.WriteEndArray();
                }
                else
                {
                    WriteElement(writer, group.Value[0]);
                }
            }
        }
        else if (attributes.Count > 0 && !string.IsNullOrWhiteSpace(element.Value))
        {
            // Element has attributes AND text content — emit text under a special key.
            writer.WriteString("#text", element.Value);
        }

        writer.WriteEndObject();
    }
}
