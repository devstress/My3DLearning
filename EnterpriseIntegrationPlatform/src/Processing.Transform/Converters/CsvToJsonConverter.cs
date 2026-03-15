using System.Text.Json;

namespace EnterpriseIntegrationPlatform.Processing.Transform.Converters;

/// <summary>
/// Converts a CSV string payload (with a header row) to a <see cref="JsonElement"/>
/// representing a JSON array of objects.
/// </summary>
/// <remarks>
/// <para>
/// The first non-empty line of the input is treated as the header row. Each subsequent
/// non-empty line is parsed as a data row. Fields are split on commas; quoted fields
/// (double-quote delimited, with doubled-quote escaping) are supported.
/// </para>
/// <para>
/// All field values in the JSON output are strings. Callers that require typed values
/// may post-process the output.
/// </para>
/// <para>
/// An empty CSV string (or a string with only a header row and no data rows) results in
/// an empty JSON array (<c>[]</c>).
/// </para>
/// </remarks>
public sealed class CsvToJsonConverter : IPayloadConverter<string, JsonElement>
{
    /// <inheritdoc />
    public Task<JsonElement> ConvertAsync(string input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var lines = input
            .Split('\n', StringSplitOptions.None)
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
        {
            return Task.FromResult(JsonDocument.Parse("[]").RootElement.Clone());
        }

        var headers = ParseCsvLine(lines[0]);

        if (headers.Count == 0)
            throw new FormatException("CSV header row contains no columns.");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartArray();

        for (var i = 1; i < lines.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fields = ParseCsvLine(lines[i]);

            writer.WriteStartObject();
            for (var col = 0; col < headers.Count; col++)
            {
                writer.WriteString(headers[col], col < fields.Count ? fields[col] : string.Empty);
            }
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.Flush();

        stream.Position = 0;
        return Task.FromResult(JsonDocument.Parse(stream).RootElement.Clone());
    }

    /// <summary>
    /// Parses a single CSV line into a list of field values, supporting double-quote
    /// delimited fields with <c>""</c> escape sequences for embedded quotes.
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < line.Length)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote ("").
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i += 2;
                    }
                    else
                    {
                        inQuotes = false;
                        i++;
                    }
                }
                else
                {
                    current.Append(c);
                    i++;
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                    i++;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                    i++;
                }
                else
                {
                    current.Append(c);
                    i++;
                }
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
