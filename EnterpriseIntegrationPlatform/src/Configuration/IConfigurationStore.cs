namespace EnterpriseIntegrationPlatform.Configuration;

/// <summary>
/// Interface for configuration CRUD operations with change notification support.
/// Implementations must be thread-safe.
/// </summary>
public interface IConfigurationStore
{
    /// <summary>
    /// Gets a configuration entry by key and environment.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="environment">Target environment (defaults to "default").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The configuration entry, or null if not found.</returns>
    Task<ConfigurationEntry?> GetAsync(string key, string environment = "default", CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a configuration entry.
    /// </summary>
    /// <param name="entry">The configuration entry to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored entry with updated version.</returns>
    Task<ConfigurationEntry> SetAsync(ConfigurationEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Deletes a configuration entry.
    /// </summary>
    /// <param name="key">The configuration key to delete.</param>
    /// <param name="environment">Target environment (defaults to "default").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the entry was deleted; false if not found.</returns>
    Task<bool> DeleteAsync(string key, string environment = "default", CancellationToken ct = default);

    /// <summary>
    /// Lists all configuration entries, optionally filtered by environment.
    /// </summary>
    /// <param name="environment">Optional environment filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All matching configuration entries.</returns>
    Task<IReadOnlyList<ConfigurationEntry>> ListAsync(string? environment = null, CancellationToken ct = default);

    /// <summary>
    /// Returns an observable stream of configuration changes for real-time notifications.
    /// </summary>
    IObservable<ConfigurationChange> WatchAsync();
}
