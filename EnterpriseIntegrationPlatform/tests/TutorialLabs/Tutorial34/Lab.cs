// ============================================================================
// Tutorial 34 – Connector.Http (Lab)
// ============================================================================
// This lab exercises InMemoryTokenCache, HttpConnectorOptions, and
// HttpConnectorAdapter to learn the HTTP connector subsystem.
// ============================================================================

using EnterpriseIntegrationPlatform.Connector.Http;
using EnterpriseIntegrationPlatform.Connectors;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial34;

[TestFixture]
public sealed class Lab
{
    // ── InMemoryTokenCache Set/Get Roundtrip ────────────────────────────────

    [Test]
    public void TokenCache_SetAndGet_Roundtrip()
    {
        var cache = new InMemoryTokenCache();

        cache.SetToken("auth", "bearer-token-123", TimeSpan.FromMinutes(5));

        var found = cache.TryGetToken("auth", out var token);

        Assert.That(found, Is.True);
        Assert.That(token, Is.EqualTo("bearer-token-123"));
    }

    // ── InMemoryTokenCache Returns False For Missing Key ────────────────────

    [Test]
    public void TokenCache_MissingKey_ReturnsFalse()
    {
        var cache = new InMemoryTokenCache();

        var found = cache.TryGetToken("nonexistent", out var token);

        Assert.That(found, Is.False);
        Assert.That(token, Is.Null);
    }

    // ── InMemoryTokenCache Expired Token Returns False ──────────────────────

    [Test]
    public void TokenCache_ExpiredToken_ReturnsFalse()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var cache = new InMemoryTokenCache(fakeTime);

        cache.SetToken("auth", "token-value", TimeSpan.FromMinutes(1));

        // Advance time past expiry
        fakeTime.Advance(TimeSpan.FromMinutes(2));

        var found = cache.TryGetToken("auth", out var token);

        Assert.That(found, Is.False);
        Assert.That(token, Is.Null);
    }

    // ── HttpConnectorOptions Defaults ────────────────────────────────────────

    [Test]
    public void HttpConnectorOptions_Defaults()
    {
        var opts = new HttpConnectorOptions();

        Assert.That(opts.BaseUrl, Is.EqualTo(string.Empty));
        Assert.That(opts.TimeoutSeconds, Is.EqualTo(30));
        Assert.That(opts.MaxRetryAttempts, Is.EqualTo(3));
        Assert.That(opts.RetryDelayMs, Is.EqualTo(1000));
        Assert.That(opts.CacheTokenExpirySeconds, Is.EqualTo(300));
        Assert.That(opts.DefaultHeaders, Is.Not.Null);
        Assert.That(opts.DefaultHeaders, Is.Empty);
    }

    // ── HttpConnectorOptions Custom Values ──────────────────────────────────

    [Test]
    public void HttpConnectorOptions_CustomValues()
    {
        var opts = new HttpConnectorOptions
        {
            BaseUrl = "https://api.example.com",
            TimeoutSeconds = 60,
            MaxRetryAttempts = 5,
            RetryDelayMs = 2000,
            CacheTokenExpirySeconds = 600,
            DefaultHeaders = new Dictionary<string, string>
            {
                ["X-Api-Key"] = "key123",
            },
        };

        Assert.That(opts.BaseUrl, Is.EqualTo("https://api.example.com"));
        Assert.That(opts.TimeoutSeconds, Is.EqualTo(60));
        Assert.That(opts.MaxRetryAttempts, Is.EqualTo(5));
        Assert.That(opts.RetryDelayMs, Is.EqualTo(2000));
        Assert.That(opts.CacheTokenExpirySeconds, Is.EqualTo(600));
        Assert.That(opts.DefaultHeaders["X-Api-Key"], Is.EqualTo("key123"));
    }

    // ── HttpConnectorAdapter.Name Property ──────────────────────────────────

    [Test]
    public void HttpConnectorAdapter_Name_Property()
    {
        var httpConnector = Substitute.For<IHttpConnector>();
        var opts = Options.Create(new HttpConnectorOptions { BaseUrl = "https://example.com" });
        var adapter = new HttpConnectorAdapter(
            "my-http-connector", httpConnector, opts,
            NullLogger<HttpConnectorAdapter>.Instance);

        Assert.That(adapter.Name, Is.EqualTo("my-http-connector"));
    }

    // ── HttpConnectorAdapter.ConnectorType Returns Http ─────────────────────

    [Test]
    public void HttpConnectorAdapter_ConnectorType_ReturnsHttp()
    {
        var httpConnector = Substitute.For<IHttpConnector>();
        var opts = Options.Create(new HttpConnectorOptions { BaseUrl = "https://example.com" });
        var adapter = new HttpConnectorAdapter(
            "test", httpConnector, opts,
            NullLogger<HttpConnectorAdapter>.Instance);

        Assert.That(adapter.ConnectorType, Is.EqualTo(ConnectorType.Http));
    }
}
