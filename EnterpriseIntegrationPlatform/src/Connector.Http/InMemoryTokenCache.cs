using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.Connector.Http;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ITokenCache"/>.
/// </summary>
public sealed class InMemoryTokenCache : ITokenCache
{
    private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset Expiry)> _cache = new();

    /// <inheritdoc />
    public bool TryGetToken(string key, out string? token)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.Expiry > DateTimeOffset.UtcNow)
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
        _cache[key] = (token, DateTimeOffset.UtcNow.Add(expiry));
    }
}
