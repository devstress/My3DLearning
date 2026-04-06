// ============================================================================
// Tutorial 34 – Connector.Http (Exam)
// ============================================================================
// Coding challenges: token caching lifecycle, custom headers in options,
// and HttpConnector construction with all dependencies.
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
public sealed class Exam
{
    // ── Challenge 1: Token Caching Lifecycle ────────────────────────────────

    [Test]
    public void Challenge1_TokenCaching_SetRetrieveVerify()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var cache = new InMemoryTokenCache(fakeTime);

        // Set a token with 10 minute expiry
        cache.SetToken("service-a", "token-aaa", TimeSpan.FromMinutes(10));

        // Verify it's cached and retrievable
        Assert.That(cache.TryGetToken("service-a", out var t1), Is.True);
        Assert.That(t1, Is.EqualTo("token-aaa"));

        // Advance time but stay within expiry
        fakeTime.Advance(TimeSpan.FromMinutes(5));
        Assert.That(cache.TryGetToken("service-a", out var t2), Is.True);
        Assert.That(t2, Is.EqualTo("token-aaa"));

        // Advance time past expiry
        fakeTime.Advance(TimeSpan.FromMinutes(6));
        Assert.That(cache.TryGetToken("service-a", out _), Is.False);

        // Set a new token
        cache.SetToken("service-a", "token-bbb", TimeSpan.FromMinutes(10));
        Assert.That(cache.TryGetToken("service-a", out var t3), Is.True);
        Assert.That(t3, Is.EqualTo("token-bbb"));
    }

    // ── Challenge 2: Custom Headers in Options ──────────────────────────────

    [Test]
    public void Challenge2_CustomHeaders_InHttpConnectorOptions()
    {
        var opts = new HttpConnectorOptions
        {
            BaseUrl = "https://api.example.com",
            DefaultHeaders = new Dictionary<string, string>
            {
                ["X-Api-Key"] = "secret-key",
                ["X-Tenant-Id"] = "tenant-123",
                ["Accept"] = "application/json",
            },
        };

        Assert.That(opts.DefaultHeaders, Has.Count.EqualTo(3));
        Assert.That(opts.DefaultHeaders["X-Api-Key"], Is.EqualTo("secret-key"));
        Assert.That(opts.DefaultHeaders["X-Tenant-Id"], Is.EqualTo("tenant-123"));
        Assert.That(opts.DefaultHeaders["Accept"], Is.EqualTo("application/json"));
    }

    // ── Challenge 3: HttpConnector Construction With All Dependencies ────────

    [Test]
    public void Challenge3_HttpConnector_ConstructionWithAllDependencies()
    {
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>())
            .Returns(new HttpClient { BaseAddress = new Uri("https://api.example.com") });

        var tokenCache = new InMemoryTokenCache();
        var options = Options.Create(new HttpConnectorOptions
        {
            BaseUrl = "https://api.example.com",
            TimeoutSeconds = 45,
            MaxRetryAttempts = 5,
        });

        var connector = new HttpConnector(
            httpClientFactory, tokenCache, options,
            NullLogger<HttpConnector>.Instance);

        Assert.That(connector, Is.Not.Null);

        // Verify the adapter wraps the connector properly
        var adapter = new HttpConnectorAdapter(
            "api-connector", connector, options,
            NullLogger<HttpConnectorAdapter>.Instance);

        Assert.That(adapter.Name, Is.EqualTo("api-connector"));
        Assert.That(adapter.ConnectorType, Is.EqualTo(ConnectorType.Http));
    }
}
