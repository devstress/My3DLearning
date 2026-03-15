using System.Text.Json;
using System.Xml.Linq;

namespace EnterpriseIntegrationPlatform.Processing.Transform.Converters;

/// <summary>
/// Converts a <see cref="JsonElement"/> payload to an XML string using the
/// <see cref="XDocument"/> representation.
/// </summary>
/// <remarks>
/// <para>
/// JSON objects are mapped to XML elements whose tag name equals the JSON property name.
/// JSON arrays are mapped to repeated elements using the parent property name (or
/// <c>item</c> when there is no parent key).
/// Scalar JSON values (string, number, boolean, null) become element text content.
/// </para>
/// <para>
/// The root XML element is always <c>&lt;root&gt;</c>.
/// </para>
/// </remarks>
public sealed class JsonToXmlConverter : IPayloadConverter<JsonElement, string>
{
    /// <inheritdoc />
    public Task<string> ConvertAsync(JsonElement input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var root = new XElement("root");
        AppendElement(root, input, "item");
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        return Task.FromResult(document.ToString());
    }

    private static void AppendElement(XElement parent, JsonElement element, string arrayItemName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        // Expand each array item as a sibling element with the property name.
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            var arrayChild = new XElement(SanitizeElementName(property.Name));
                            AppendElement(arrayChild, item, "item");
                            parent.Add(arrayChild);
                        }
                    }
                    else
                    {
                        var child = new XElement(SanitizeElementName(property.Name));
                        AppendElement(child, property.Value, property.Name);
                        parent.Add(child);
                    }
                }
                break;

            case JsonValueKind.Array:
                // Top-level or nested plain array: emit each item with arrayItemName.
                foreach (var item in element.EnumerateArray())
                {
                    var child = new XElement(SanitizeElementName(arrayItemName));
                    AppendElement(child, item, "item");
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
                parent.SetAttributeValue("nil", "true");
                break;
        }
    }

    /// <summary>
    /// Ensures the element name is valid XML. Replaces characters that are not allowed
    /// in XML element names with underscores and prepends an underscore when the name
    /// starts with a digit.
    /// </summary>
    private static string SanitizeElementName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "_";

        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
                chars[i] = '_';
        }

        var result = new string(chars);
        return char.IsDigit(result[0]) ? '_' + result : result;
    }
}
