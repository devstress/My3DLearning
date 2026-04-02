namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Service responsible for rotating secrets on a defined schedule.
/// Implementations monitor rotation policies and trigger rotation when secrets approach expiry.
/// </summary>
public interface ISecretRotationService
{
    /// <summary>
    /// Registers a rotation policy for a secret key.
    /// </summary>
    /// <param name="key">The secret key to rotate.</param>
    /// <param name="policy">The rotation policy defining schedule and behavior.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RegisterPolicyAsync(string key, SecretRotationPolicy policy, CancellationToken ct = default);

    /// <summary>
    /// Removes a rotation policy for a secret key.
    /// </summary>
    /// <param name="key">The secret key whose policy should be removed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the policy was removed; false if no policy existed for the key.</returns>
    Task<bool> UnregisterPolicyAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Gets the rotation policy registered for a secret key.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The rotation policy, or null if no policy is registered.</returns>
    Task<SecretRotationPolicy?> GetPolicyAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Forces immediate rotation of a secret regardless of its policy schedule.
    /// </summary>
    /// <param name="key">The secret key to rotate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new secret entry after rotation.</returns>
    Task<SecretEntry> RotateNowAsync(string key, CancellationToken ct = default);
}
