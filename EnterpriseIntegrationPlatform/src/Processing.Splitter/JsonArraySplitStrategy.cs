using System.Text.Json;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Splitter;

/// <summary>
/// An <see cref="ISplitStrategy{T}"/> that splits a <see cref="JsonElement"/> payload
/// containing a JSON array into individual <see cref="JsonElement"/> items.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="SplitterOptions.ArrayPropertyName"/> is set, the strategy navigates
/// to that property within the JSON object and splits the array found there. When not set,
/// the strategy expects the payload itself to be a top-level JSON array.
/// </para>
/// <para>
/// Each array element is cloned (via round-trip serialization) to ensure it is independent
/// of the source <see cref="JsonDocument"/> lifetime.
/// </para>
/// </remarks>
public sealed class JsonArraySplitStrategy : ISplitStrategy<JsonElement>
{
    private readonly string? _arrayPropertyName;

    /// <summary>
    /// Initialises a new instance of <see cref="JsonArraySplitStrategy"/> using the
    /// array property name from the supplied <paramref name="options"/>.
    /// </summary>
    public JsonArraySplitStrategy(IOptions<SplitterOptions> options)
    {
        _arrayPropertyName = options.Value.ArrayPropertyName;
    }

    /// <inheritdoc />
    public IReadOnlyList<JsonElement> Split(JsonElement composite)
    {
        var target = ResolveArray(composite);

        if (target.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(_arrayPropertyName)
                    ? "JsonArraySplitStrategy: payload is not a JSON array. " +
                      "Set SplitterOptions.ArrayPropertyName to specify the array property within a JSON object."
                    : $"JsonArraySplitStrategy: property '{_arrayPropertyName}' is not a JSON array.");
        }

        var items = new List<JsonElement>(target.GetArrayLength());

        foreach (var element in target.EnumerateArray())
        {
            // Clone the element to decouple it from the source JsonDocument.
            items.Add(JsonSerializer.SerializeToElement(element));
        }

        return items;
    }

    private JsonElement ResolveArray(JsonElement root)
    {
        if (string.IsNullOrWhiteSpace(_arrayPropertyName))
            return root;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"JsonArraySplitStrategy: payload is not a JSON object — " +
                $"cannot navigate to property '{_arrayPropertyName}'.");
        }

        if (!root.TryGetProperty(_arrayPropertyName, out var property))
        {
            throw new InvalidOperationException(
                $"JsonArraySplitStrategy: property '{_arrayPropertyName}' not found in the payload.");
        }

        return property;
    }
}
