using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ISecretProvider"/> for development and testing.
/// Stores secrets with versioning in a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class InMemorySecretProvider : ISecretProvider
{
    private readonly ConcurrentDictionary<string, SortedList<int, SecretEntry>> _secrets = new();
    private readonly SecretAuditLogger? _auditLogger;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemorySecretProvider"/>.
    /// </summary>
    /// <param name="auditLogger">Optional audit logger for recording access events.</param>
    public InMemorySecretProvider(SecretAuditLogger? auditLogger = null)
    {
        _auditLogger = auditLogger;
    }

    /// <inheritdoc />
    public Task<SecretEntry?> GetSecretAsync(string key, string? version = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_secrets.TryGetValue(key, out var versions) || versions.Count == 0)
        {
            _auditLogger?.LogRead(key, version, success: false);
            return Task.FromResult<SecretEntry?>(null);
        }

        SecretEntry? entry;
        if (version is not null && int.TryParse(version, out var versionNumber))
        {
            versions.TryGetValue(versionNumber, out entry);
        }
        else
        {
            entry = versions.Values[^1];
        }

        _auditLogger?.LogRead(key, entry?.Version, success: entry is not null);
        return Task.FromResult(entry);
    }

    /// <inheritdoc />
    public Task<SecretEntry> SetSecretAsync(string key, string value, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var versions = _secrets.GetOrAdd(key, _ => new SortedList<int, SecretEntry>());

        SecretEntry entry;
        lock (versions)
        {
            var nextVersion = versions.Count > 0 ? versions.Keys[^1] + 1 : 1;
            entry = new SecretEntry(
                key,
                value,
                nextVersion.ToString(),
                DateTimeOffset.UtcNow,
                Metadata: metadata);
            versions[nextVersion] = entry;
        }

        _auditLogger?.LogWrite(key, entry.Version);
        return Task.FromResult(entry);
    }

    /// <inheritdoc />
    public Task<bool> DeleteSecretAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var removed = _secrets.TryRemove(key, out _);
        _auditLogger?.LogDelete(key, success: removed);
        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListSecretKeysAsync(string? prefix = null, CancellationToken ct = default)
    {
        IReadOnlyList<string> keys = prefix is null
            ? _secrets.Keys.ToList()
            : _secrets.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult(keys);
    }
}
