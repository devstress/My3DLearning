namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Represents a secret value with its associated version, expiry, and metadata.
/// </summary>
/// <param name="Key">The secret key or path.</param>
/// <param name="Value">The secret value.</param>
/// <param name="Version">Version identifier for this secret revision.</param>
/// <param name="CreatedAt">UTC timestamp when this version was created.</param>
/// <param name="ExpiresAt">Optional UTC timestamp when this secret version expires.</param>
/// <param name="Metadata">Optional metadata key-value pairs associated with the secret.</param>
public sealed record SecretEntry(
    string Key,
    string Value,
    string Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt = null,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    /// <summary>
    /// Returns true if this secret has expired based on the current UTC time.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;
}
