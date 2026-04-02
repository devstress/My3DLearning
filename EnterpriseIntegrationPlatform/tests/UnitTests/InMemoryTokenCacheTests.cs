using EnterpriseIntegrationPlatform.Connector.Http;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class InMemoryTokenCacheTests
{
    [Test]
    public void TryGetToken_NonExistentKey_ReturnsFalse()
    {
        var cache = new InMemoryTokenCache();

        var result = cache.TryGetToken("missing-key", out var token);

        Assert.That(result, Is.False);
        Assert.That(token, Is.Null);
    }

    [Test]
    public void SetToken_ValidKeyAndToken_TryGetTokenReturnsToken()
    {
        var cache = new InMemoryTokenCache();
        cache.SetToken("my-endpoint", "abc123", TimeSpan.FromMinutes(5));

        var result = cache.TryGetToken("my-endpoint", out var token);

        Assert.That(result, Is.True);
        Assert.That(token, Is.EqualTo("abc123"));
    }

    [Test]
    public void TryGetToken_ExpiredEntry_ReturnsFalse()
    {
        var cache = new InMemoryTokenCache();
        // Zero expiry means the token is stored with Expiry == UtcNow,
        // and the check (Expiry > UtcNow) is immediately false.
        cache.SetToken("expiring-key", "token", TimeSpan.Zero);

        var result = cache.TryGetToken("expiring-key", out var token);

        Assert.That(result, Is.False);
        Assert.That(token, Is.Null);
    }

    [Test]
    public void SetToken_ExistingKey_OverridesValue()
    {
        var cache = new InMemoryTokenCache();
        cache.SetToken("endpoint", "first-token", TimeSpan.FromMinutes(5));
        cache.SetToken("endpoint", "second-token", TimeSpan.FromMinutes(5));

        cache.TryGetToken("endpoint", out var token);

        Assert.That(token, Is.EqualTo("second-token"));
    }

    [Test]
    public void MultipleKeys_AreIsolated()
    {
        var cache = new InMemoryTokenCache();
        cache.SetToken("key-a", "token-a", TimeSpan.FromMinutes(5));
        cache.SetToken("key-b", "token-b", TimeSpan.FromMinutes(5));

        cache.TryGetToken("key-a", out var tokenA);
        cache.TryGetToken("key-b", out var tokenB);

        Assert.That(tokenA, Is.EqualTo("token-a"));
        Assert.That(tokenB, Is.EqualTo("token-b"));
    }

    [Test]
    public void ConcurrentSetGet_DoesNotThrow()
    {
        var cache = new InMemoryTokenCache();

        var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            cache.SetToken($"key-{i % 5}", $"token-{i}", TimeSpan.FromMinutes(5));
            cache.TryGetToken($"key-{i % 5}", out _);
        }));

        var act = () => Task.WhenAll(tasks).GetAwaiter().GetResult();

        Assert.DoesNotThrow(() => act());
    }
}
