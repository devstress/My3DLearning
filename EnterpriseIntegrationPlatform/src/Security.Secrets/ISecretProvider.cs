namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Provides access to secrets stored in an external secrets manager.
/// Implementations must be thread-safe.
/// </summary>
public interface ISecretProvider
{
    /// <summary>
    /// Retrieves a secret by its key.
    /// </summary>
    /// <param name="key">The secret key or path.</param>
    /// <param name="version">Optional version identifier. When null, the latest version is returned.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The secret entry, or null if the secret does not exist.</returns>
    Task<SecretEntry?> GetSecretAsync(string key, string? version = null, CancellationToken ct = default);

    /// <summary>
    /// Stores or updates a secret.
    /// </summary>
    /// <param name="key">The secret key or path.</param>
    /// <param name="value">The secret value.</param>
    /// <param name="metadata">Optional metadata key-value pairs to associate with the secret.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored secret entry with updated version information.</returns>
    Task<SecretEntry> SetSecretAsync(string key, string value, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes a secret by its key.
    /// </summary>
    /// <param name="key">The secret key or path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the secret was deleted; false if it did not exist.</returns>
    Task<bool> DeleteSecretAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Lists all secret keys available in the provider.
    /// </summary>
    /// <param name="prefix">Optional prefix filter for key names.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of secret keys matching the prefix.</returns>
    Task<IReadOnlyList<string>> ListSecretKeysAsync(string? prefix = null, CancellationToken ct = default);
}
