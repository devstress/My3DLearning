using System.Text.Json;
using System.Xml.Linq;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Transform step that converts an XML payload to JSON. XML elements are mapped to
/// JSON properties; repeated sibling elements with the same name become JSON arrays.
/// </summary>
/// <remarks>
/// <para>
/// The conversion produces a JSON object whose keys correspond to the child element
/// names of the XML root. Attributes are included with an <c>@</c> prefix. Text-only
/// elements are represented as string values; elements with both attributes and text use
/// <c>#text</c> for the text content.
/// </para>
/// <para>
/// The output content type is set to <c>application/json</c>.
/// </para>
/// </remarks>
public sealed class XmlToJsonStep : ITransformStep
{
    /// <inheritdoc />
    public string Name => "XmlToJson";

    /// <inheritdoc />
    public Task<TransformContext> ExecuteAsync(
        TransformContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var xDoc = XDocument.Parse(context.Payload);
        if (xDoc.Root is null)
            throw new InvalidOperationException("XML document has no root element.");

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            WriteElement(writer, xDoc.Root);
            writer.WriteEndObject();
        }

        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        var result = context.WithPayload(json, "application/json");
        result.Metadata[$"Step.{Name}.Applied"] = "true";
        return Task.FromResult(result);
    }

    private static void WriteElement(Utf8JsonWriter writer, XElement element)
    {
        // Group child elements by name to detect arrays (repeated siblings).
        var childGroups = element.Elements()
            .GroupBy(e => e.Name.LocalName)
            .ToList();

        var hasAttributes = element.Attributes().Any();
        var hasChildElements = childGroups.Count > 0;
        var textValue = hasChildElements ? null : element.Value;

        // Write attributes with @ prefix.
        foreach (var attr in element.Attributes())
        {
            if (attr.IsNamespaceDeclaration)
                continue;
            writer.WriteString($"@{attr.Name.LocalName}", attr.Value);
        }

        // If the element has both attributes and text (but no child elements), write text as #text.
        if (hasAttributes && !hasChildElements && !string.IsNullOrEmpty(textValue))
        {
            writer.WriteString("#text", textValue);
            return;
        }

        // Write child elements.
        foreach (var group in childGroups)
        {
            var items = group.ToList();

            if (items.Count > 1)
            {
                // Repeated elements → JSON array.
                writer.WriteStartArray(group.Key);
                foreach (var child in items)
                {
                    WriteChildValue(writer, child);
                }
                writer.WriteEndArray();
            }
            else
            {
                var child = items[0];
                writer.WritePropertyName(group.Key);
                WriteChildValue(writer, child);
            }
        }

        // Leaf text value (no attributes, no child elements).
        if (!hasAttributes && !hasChildElements && textValue is not null)
        {
            // Already handled by the caller through WriteString.
        }
    }

    private static void WriteChildValue(Utf8JsonWriter writer, XElement child)
    {
        var hasChildren = child.HasElements || child.Attributes().Any(a => !a.IsNamespaceDeclaration);

        if (hasChildren)
        {
            writer.WriteStartObject();
            WriteElement(writer, child);
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStringValue(child.Value);
        }
    }
}
