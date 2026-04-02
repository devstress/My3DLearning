using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.Configuration;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IConfigurationStore"/>
/// using <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Supports real-time change notifications via <see cref="IObservable{T}"/>.
/// </summary>
public sealed class InMemoryConfigurationStore : IConfigurationStore
{
    private readonly ConcurrentDictionary<string, ConfigurationEntry> _entries = new();
    private readonly ConfigurationChangeNotifier _notifier;

    public InMemoryConfigurationStore(ConfigurationChangeNotifier notifier)
    {
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
    }

    /// <inheritdoc />
    public Task<ConfigurationEntry?> GetAsync(string key, string environment = "default", CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var compositeKey = BuildKey(key, environment);
        _entries.TryGetValue(compositeKey, out var entry);
        return Task.FromResult(entry);
    }

    /// <inheritdoc />
    public Task<ConfigurationEntry> SetAsync(ConfigurationEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Key);

        var compositeKey = BuildKey(entry.Key, entry.Environment);

        var updated = _entries.AddOrUpdate(
            compositeKey,
            _ =>
            {
                var created = entry with { Version = 1, LastModified = DateTimeOffset.UtcNow };
                _notifier.Publish(new ConfigurationChange(
                    entry.Key, entry.Environment, ConfigurationChangeType.Created,
                    null, created.Value, DateTimeOffset.UtcNow));
                return created;
            },
            (_, existing) =>
            {
                var newVersion = entry with
                {
                    Version = existing.Version + 1,
                    LastModified = DateTimeOffset.UtcNow
                };
                _notifier.Publish(new ConfigurationChange(
                    entry.Key, entry.Environment, ConfigurationChangeType.Updated,
                    existing.Value, newVersion.Value, DateTimeOffset.UtcNow));
                return newVersion;
            });

        return Task.FromResult(updated);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string key, string environment = "default", CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var compositeKey = BuildKey(key, environment);

        if (_entries.TryRemove(compositeKey, out var removed))
        {
            _notifier.Publish(new ConfigurationChange(
                key, environment, ConfigurationChangeType.Deleted,
                removed.Value, null, DateTimeOffset.UtcNow));
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ConfigurationEntry>> ListAsync(string? environment = null, CancellationToken ct = default)
    {
        IReadOnlyList<ConfigurationEntry> result = environment is null
            ? _entries.Values.ToList()
            : _entries.Values.Where(e => e.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public IObservable<ConfigurationChange> WatchAsync() => _notifier;

    private static string BuildKey(string key, string environment) =>
        $"{environment.ToLowerInvariant()}::{key.ToLowerInvariant()}";
}
