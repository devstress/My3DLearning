namespace EnterpriseIntegrationPlatform.Security;

/// <summary>
/// Configuration options for JWT bearer authentication.
/// Bind from the <c>Jwt</c> configuration section.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// JWT issuer. Must match the <c>iss</c> claim in incoming tokens.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// JWT audience. Must match the <c>aud</c> claim in incoming tokens.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Symmetric signing key (Base64-encoded). Used for HS256/HS512 validation.
    /// In production use a secrets manager; never commit this value to source control.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c>, the token lifetime (<c>exp</c>) is validated.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance for token expiry validation. Defaults to 5 minutes.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
}
