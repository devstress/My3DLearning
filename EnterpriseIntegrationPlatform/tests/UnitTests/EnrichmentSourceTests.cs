using System.Text.Json.Nodes;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public sealed class EnrichmentSourceTests
{
    // ───── CachedEnrichmentSource ─────

    [Test]
    public async Task CachedSource_CacheMiss_DelegatesToInner()
    {
        var inner = Substitute.For<IEnrichmentSource>();
        inner.FetchAsync("key-1", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"name":"Alice"}"""));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedEnrichmentSource(
            inner, cache, TimeSpan.FromMinutes(5),
            NullLogger<CachedEnrichmentSource>.Instance);

        var result = await cached.FetchAsync("key-1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["name"]!.GetValue<string>(), Is.EqualTo("Alice"));
        await inner.Received(1).FetchAsync("key-1", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CachedSource_CacheHit_DoesNotCallInnerAgain()
    {
        var inner = Substitute.For<IEnrichmentSource>();
        inner.FetchAsync("key-1", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"name":"Alice"}"""));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedEnrichmentSource(
            inner, cache, TimeSpan.FromMinutes(5),
            NullLogger<CachedEnrichmentSource>.Instance);

        await cached.FetchAsync("key-1");
        var result = await cached.FetchAsync("key-1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["name"]!.GetValue<string>(), Is.EqualTo("Alice"));
        await inner.Received(1).FetchAsync("key-1", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CachedSource_ExpiredEntry_CallsInnerAgain()
    {
        var inner = Substitute.For<IEnrichmentSource>();
        inner.FetchAsync("key-1", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"v":1}"""), JsonNode.Parse("""{"v":2}"""));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedEnrichmentSource(
            inner, cache, TimeSpan.FromMilliseconds(1),
            NullLogger<CachedEnrichmentSource>.Instance);

        await cached.FetchAsync("key-1");
        await Task.Delay(20); // let cache expire
        var result = await cached.FetchAsync("key-1");

        Assert.That(result, Is.Not.Null);
        await inner.Received(2).FetchAsync("key-1", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CachedSource_NullResult_CachesNull()
    {
        var inner = Substitute.For<IEnrichmentSource>();
        inner.FetchAsync("missing", Arg.Any<CancellationToken>())
            .Returns((JsonNode?)null);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedEnrichmentSource(
            inner, cache, TimeSpan.FromMinutes(5),
            NullLogger<CachedEnrichmentSource>.Instance);

        var r1 = await cached.FetchAsync("missing");
        var r2 = await cached.FetchAsync("missing");

        Assert.That(r1, Is.Null);
        Assert.That(r2, Is.Null);
        await inner.Received(1).FetchAsync("missing", Arg.Any<CancellationToken>());
    }

    // ───── IEnrichmentSource integration with ContentEnricher ─────

    [Test]
    public async Task ContentEnricher_WithCustomSource_MergesData()
    {
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("C-42", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"name":"Alice","tier":"gold"}"""));

        var enricher = new ContentEnricher(
            source,
            Microsoft.Extensions.Options.Options.Create(new ContentEnricherOptions
            {
                EndpointUrlTemplate = "unused",
                LookupKeyPath = "customerId",
                MergeTargetPath = "customer",
            }),
            NullLogger<ContentEnricher>.Instance);

        var result = await enricher.EnrichAsync("""{"customerId":"C-42"}""", Guid.NewGuid());
        var node = JsonNode.Parse(result);

        Assert.That(node!["customer"]!["name"]!.GetValue<string>(), Is.EqualTo("Alice"));
        Assert.That(node["customer"]!["tier"]!.GetValue<string>(), Is.EqualTo("gold"));
    }
}
