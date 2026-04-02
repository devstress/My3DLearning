namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Represents a structured audit event for secret access or rotation.
/// </summary>
/// <param name="Action">The type of operation performed.</param>
/// <param name="SecretKey">The key of the secret that was accessed.</param>
/// <param name="Timestamp">UTC timestamp of the event.</param>
/// <param name="Principal">The identity that performed the operation, if known.</param>
/// <param name="Version">The secret version involved, if applicable.</param>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Detail">Additional detail about the operation.</param>
public sealed record SecretAuditEvent(
    SecretAccessAction Action,
    string SecretKey,
    DateTimeOffset Timestamp,
    string? Principal = null,
    string? Version = null,
    bool Success = true,
    string? Detail = null);
