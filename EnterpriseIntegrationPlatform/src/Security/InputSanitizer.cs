using System.Text.RegularExpressions;
using System.Web;

namespace EnterpriseIntegrationPlatform.Security;

/// <summary>
/// Production implementation of <see cref="IInputSanitizer"/> that removes CRLF characters,
/// null bytes, script tags, inline event handlers, SQL injection patterns, HTML entities,
/// and Unicode direction override characters.
/// </summary>
public sealed partial class InputSanitizer : IInputSanitizer
{
    // Characters that must never appear in sanitized output.
    private static readonly char[] DangerousChars = ['\r', '\n', '\0'];

    // Unicode direction override characters (U+202A–U+202E, U+2066–U+2069).
    private static readonly char[] UnicodeOverrides =
    [
        '\u202A', '\u202B', '\u202C', '\u202D', '\u202E',
        '\u2066', '\u2067', '\u2068', '\u2069',
    ];

    // Source-generated regexes for pattern matching (thread-safe, compiled).
    [GeneratedRegex(@"<script[\s>].*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ScriptBlockRegex();

    [GeneratedRegex(@"\bon\w+\s*=", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex InlineEventHandlerRegex();

    [GeneratedRegex(@"(?:';\s*DROP\s+TABLE|(?:^|\s)OR\s+1\s*=\s*1|UNION\s+SELECT)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SqlInjectionRegex();

    /// <inheritdoc />
    public string Sanitize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var result = input;

        // 1. Decode HTML entities to neutralize entity-based bypasses (e.g. &#60; → <).
        result = HttpUtility.HtmlDecode(result);

        // 2. Strip <script>…</script> blocks.
        result = ScriptBlockRegex().Replace(result, string.Empty);

        // 3. Remove inline event handlers (onclick=, onerror=, etc.).
        result = InlineEventHandlerRegex().Replace(result, string.Empty);

        // 4. Remove SQL injection patterns.
        result = SqlInjectionRegex().Replace(result, string.Empty);

        // 5. Replace CRLF with space, remove null bytes.
        result = result
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace("\0", string.Empty, StringComparison.Ordinal);

        // 6. Remove Unicode direction override characters.
        var overrideSet = new HashSet<char>(UnicodeOverrides);
        if (result.Any(c => overrideSet.Contains(c)))
            result = new string(result.Where(c => !overrideSet.Contains(c)).ToArray());

        return result.Trim();
    }

    /// <inheritdoc />
    public bool IsClean(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Check for CRLF and null bytes.
        if (input.IndexOfAny(DangerousChars) >= 0)
            return false;

        // Check for Unicode direction overrides.
        if (input.IndexOfAny(UnicodeOverrides) >= 0)
            return false;

        // Check for script tags.
        if (ScriptBlockRegex().IsMatch(input))
            return false;

        // Check for inline event handlers.
        if (InlineEventHandlerRegex().IsMatch(input))
            return false;

        // Check for SQL injection patterns.
        if (SqlInjectionRegex().IsMatch(input))
            return false;

        // Check for HTML entities that could bypass filters.
        if (input.Contains("&#", StringComparison.Ordinal) ||
            input.Contains("&lt;", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("&gt;", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
