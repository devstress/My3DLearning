namespace EnterpriseIntegrationPlatform.Security;

/// <summary>
/// Sanitizes string inputs to prevent injection attacks and remove dangerous characters.
/// </summary>
public interface IInputSanitizer
{
    /// <summary>
    /// Removes or escapes characters that could be used for injection attacks.
    /// Returns a safe version of the input suitable for logging and processing.
    /// </summary>
    /// <param name="input">The raw input string.</param>
    /// <returns>
    /// A sanitized string with CRLF characters replaced, null bytes removed,
    /// and the result trimmed.
    /// </returns>
    string Sanitize(string input);

    /// <summary>
    /// Validates that a string does not contain disallowed characters.
    /// Returns <c>true</c> when the input is safe; <c>false</c> otherwise.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    bool IsClean(string input);
}
