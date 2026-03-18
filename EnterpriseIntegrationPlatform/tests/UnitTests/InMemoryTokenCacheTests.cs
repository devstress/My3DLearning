using EnterpriseIntegrationPlatform.Connector.Http;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class InMemoryTokenCacheTests
{
    [Fact]
    public void TryGetToken_NonExistentKey_ReturnsFalse()
    {
        var cache = new InMemoryTokenCache();

        var result = cache.TryGetToken("missing-key", out var token);

        result.Should().BeFalse();
        token.Should().BeNull();
    }

    [Fact]
    public void SetToken_ValidKeyAndToken_TryGetTokenReturnsToken()
    {
        var cache = new InMemoryTokenCache();
        cache.SetToken("my-endpoint", "abc123", TimeSpan.FromMinutes(5));

        var result = cache.TryGetToken("my-endpoint", out var token);

        result.Should().BeTrue();
        token.Should().Be("abc123");
    }

    [Fact]
    public void TryGetToken_ExpiredEntry_ReturnsFalse()
    {
        var cache = new InMemoryTokenCache();
        cache.SetToken("expiring-key", "token", TimeSpan.FromMilliseconds(1));

        Thread.Sleep(20);

        var result = cache.TryGetToken("expiring-key", out var token);

        result.Should().BeFalse();
        token.Should().BeNull();
    }

    [Fact]
    public void SetToken_ExistingKey_OverridesValue()
    {
        var cache = new InMemoryTokenCache();
        cache.SetToken("endpoint", "first-token", TimeSpan.FromMinutes(5));
        cache.SetToken("endpoint", "second-token", TimeSpan.FromMinutes(5));

        cache.TryGetToken("endpoint", out var token);

        token.Should().Be("second-token");
    }

    [Fact]
    public void MultipleKeys_AreIsolated()
    {
        var cache = new InMemoryTokenCache();
        cache.SetToken("key-a", "token-a", TimeSpan.FromMinutes(5));
        cache.SetToken("key-b", "token-b", TimeSpan.FromMinutes(5));

        cache.TryGetToken("key-a", out var tokenA);
        cache.TryGetToken("key-b", out var tokenB);

        tokenA.Should().Be("token-a");
        tokenB.Should().Be("token-b");
    }

    [Fact]
    public void ConcurrentSetGet_DoesNotThrow()
    {
        var cache = new InMemoryTokenCache();

        var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            cache.SetToken($"key-{i % 5}", $"token-{i}", TimeSpan.FromMinutes(5));
            cache.TryGetToken($"key-{i % 5}", out _);
        }));

        var act = () => Task.WhenAll(tasks).GetAwaiter().GetResult();

        act.Should().NotThrow();
    }
}
