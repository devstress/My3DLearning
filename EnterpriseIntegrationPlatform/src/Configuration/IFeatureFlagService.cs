namespace EnterpriseIntegrationPlatform.Configuration;

/// <summary>
/// Service for managing and evaluating feature flags.
/// Implementations must be thread-safe.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks whether a feature flag is enabled, accounting for rollout percentage and tenant targeting.
    /// </summary>
    /// <param name="name">Feature flag name.</param>
    /// <param name="tenantId">Optional tenant ID for targeted evaluation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the feature is enabled for the given context.</returns>
    Task<bool> IsEnabledAsync(string name, string? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the variant value for a feature flag.
    /// </summary>
    /// <param name="name">Feature flag name.</param>
    /// <param name="variantKey">The variant key to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The variant value, or null if the flag or variant is not found.</returns>
    Task<string?> GetVariantAsync(string name, string variantKey, CancellationToken ct = default);

    /// <summary>
    /// Gets a feature flag by name.
    /// </summary>
    /// <param name="name">Feature flag name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The feature flag, or null if not found.</returns>
    Task<FeatureFlag?> GetAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a feature flag.
    /// </summary>
    /// <param name="flag">The feature flag to store.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetAsync(FeatureFlag flag, CancellationToken ct = default);

    /// <summary>
    /// Deletes a feature flag.
    /// </summary>
    /// <param name="name">Feature flag name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the flag was deleted; false if not found.</returns>
    Task<bool> DeleteAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Lists all feature flags.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All registered feature flags.</returns>
    Task<IReadOnlyList<FeatureFlag>> ListAsync(CancellationToken ct = default);
}
