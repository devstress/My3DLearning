namespace EnterpriseIntegrationPlatform.Connector.Http;

/// <summary>
/// Cache for short-lived authentication tokens to avoid redundant token endpoint calls.
/// </summary>
public interface ITokenCache
{
    /// <summary>
    /// Attempts to retrieve a non-expired token for the given key.
    /// </summary>
    /// <param name="key">The cache key (typically the token endpoint URL).</param>
    /// <param name="token">The cached token value, or <c>null</c> when the method returns <c>false</c>.</param>
    /// <returns><c>true</c> if a valid, non-expired token was found; otherwise <c>false</c>.</returns>
    bool TryGetToken(string key, out string? token);

    /// <summary>
    /// Stores a token under the given key with the specified expiry duration.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="token">The token value to cache.</param>
    /// <param name="expiry">How long the token should remain valid.</param>
    void SetToken(string key, string token, TimeSpan expiry);
}
