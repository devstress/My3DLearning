using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.Connector.Http;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ITokenCache"/>.
/// </summary>
public sealed class InMemoryTokenCache : ITokenCache
{
    private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset Expiry)> _cache = new();
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises a new <see cref="InMemoryTokenCache"/> using the system clock.
    /// </summary>
    public InMemoryTokenCache() : this(TimeProvider.System) { }

    /// <summary>
    /// Initialises a new <see cref="InMemoryTokenCache"/> with an injectable <see cref="TimeProvider"/>.
    /// </summary>
    /// <param name="timeProvider">The time provider to use for expiry calculations.</param>
    public InMemoryTokenCache(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public bool TryGetToken(string key, out string? token)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.Expiry > _timeProvider.GetUtcNow())
            {
                token = entry.Token;
                return true;
            }

            _cache.TryRemove(key, out _);
        }

        token = null;
        return false;
    }

    /// <inheritdoc />
    public void SetToken(string key, string token, TimeSpan expiry)
    {
        _cache[key] = (token, _timeProvider.GetUtcNow().Add(expiry));
    }
}
