namespace EnterpriseIntegrationPlatform.Security;

/// <summary>
/// Production implementation of <see cref="IInputSanitizer"/> that removes CRLF characters,
/// null bytes, and other characters dangerous for injection attacks.
/// </summary>
public sealed class InputSanitizer : IInputSanitizer
{
    // Characters that must never appear in sanitized output.
    private static readonly char[] DangerousChars = ['\r', '\n', '\0'];

    /// <inheritdoc />
    public string Sanitize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        // Replace CRLF with space (preserves readability in logs), remove null bytes.
        var result = input
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace("\0", string.Empty, StringComparison.Ordinal);
        return result.Trim();
    }

    /// <inheritdoc />
    public bool IsClean(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.IndexOfAny(DangerousChars) < 0;
    }
}
