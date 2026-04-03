using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Transform step that converts a JSON payload to XML. The resulting XML uses a
/// configurable root element name and maps JSON properties to XML elements.
/// </summary>
/// <remarks>
/// <para>
/// Supports JSON objects and arrays at the root level. Object properties become child
/// elements; array items become repeated elements named <c>Item</c>. Nested objects and
/// arrays are handled recursively.
/// </para>
/// <para>
/// The output content type is set to <c>application/xml</c>.
/// </para>
/// </remarks>
public sealed class JsonToXmlStep : ITransformStep
{
    private readonly string _rootElementName;

    /// <summary>
    /// Initialises a new <see cref="JsonToXmlStep"/> with the specified root element name.
    /// </summary>
    /// <param name="rootElementName">
    /// Name of the XML root element. Defaults to <c>Root</c>.
    /// </param>
    public JsonToXmlStep(string rootElementName = "Root")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootElementName);
        _rootElementName = rootElementName;
    }

    /// <inheritdoc />
    public string Name => "JsonToXml";

    /// <inheritdoc />
    public Task<TransformContext> ExecuteAsync(
        TransformContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var doc = JsonDocument.Parse(context.Payload);
        var root = new XElement(_rootElementName);
        ConvertElement(root, doc.RootElement);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false,
        };

        using var sw = new StringWriter();
        using (var xw = XmlWriter.Create(sw, settings))
        {
            root.WriteTo(xw);
        }

        var result = context.WithPayload(sw.ToString(), "application/xml");
        result.Metadata[$"Step.{Name}.Applied"] = "true";
        return Task.FromResult(result);
    }

    private static void ConvertElement(XElement parent, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var child = new XElement(SanitizeElementName(property.Name));
                    ConvertElement(child, property.Value);
                    parent.Add(child);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var child = new XElement("Item");
                    ConvertElement(child, item);
                    parent.Add(child);
                }
                break;

            case JsonValueKind.String:
                parent.Value = element.GetString() ?? string.Empty;
                break;

            case JsonValueKind.Number:
                parent.Value = element.GetRawText();
                break;

            case JsonValueKind.True:
                parent.Value = "true";
                break;

            case JsonValueKind.False:
                parent.Value = "false";
                break;

            case JsonValueKind.Null:
                break;

            default:
                parent.Value = element.GetRawText();
                break;
        }
    }

    /// <summary>
    /// Ensures a JSON property name is a valid XML element name by replacing invalid
    /// leading characters with an underscore prefix and replacing other invalid characters
    /// with underscores.
    /// </summary>
    private static string SanitizeElementName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "_";

        var chars = name.ToCharArray();

        if (!XmlConvert.IsStartNCNameChar(chars[0]))
            chars[0] = '_';

        for (var i = 1; i < chars.Length; i++)
        {
            if (!XmlConvert.IsNCNameChar(chars[i]))
                chars[i] = '_';
        }

        return new string(chars);
    }
}
