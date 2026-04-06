// ============================================================================
// Tutorial 18 – Content Enricher (Lab)
// ============================================================================
// EIP Pattern: Content Enricher.
// E2E: ContentEnricher with NSubstitute IEnrichmentSource, verify enriched
// JSON payload, fallback behaviour, and publish via MockEndpoint.
// ============================================================================

using System.Text.Json.Nodes;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial18;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("enricher-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    [Test]
    public async Task Enrich_MergesExternalData()
    {
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("C-100", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"name":"Alice","tier":"Gold"}"""));

        var enricher = CreateEnricher(source, "order.customerId", "customer");

        var payload = """{"order":{"customerId":"C-100","total":250}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        Assert.That(result, Does.Contain("Alice"));
        Assert.That(result, Does.Contain("Gold"));
        Assert.That(result, Does.Contain("C-100"));
    }

    [Test]
    public async Task Enrich_NestedLookup_ExtractsCorrectKey()
    {
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("P-200", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"sku":"Widget","warehouse":"WH-1"}"""));

        var enricher = CreateEnricher(source, "line.productId", "product");

        var payload = """{"line":{"productId":"P-200","qty":5}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        Assert.That(result, Does.Contain("Widget"));
        Assert.That(result, Does.Contain("WH-1"));
    }

    [Test]
    public async Task Enrich_SourceReturnsNull_UsesFallback()
    {
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((JsonNode?)null);

        var options = new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "order.customerId",
            MergeTargetPath = "customer",
            FallbackOnFailure = true,
            FallbackValue = """{"name":"Unknown","tier":"None"}""",
        };
        var enricher = new ContentEnricher(
            source, Options.Create(options),
            NullLogger<ContentEnricher>.Instance);

        var payload = """{"order":{"customerId":"C-999","total":10}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        Assert.That(result, Does.Contain("Unknown"));
        Assert.That(result, Does.Contain("None"));
    }

    [Test]
    public async Task Enrich_MissingLookupKey_FallsBack()
    {
        var source = Substitute.For<IEnrichmentSource>();
        var options = new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "order.customerId",
            MergeTargetPath = "customer",
            FallbackOnFailure = true,
            FallbackValue = """{"name":"Fallback"}""",
        };
        var enricher = new ContentEnricher(
            source, Options.Create(options),
            NullLogger<ContentEnricher>.Instance);

        var payload = """{"order":{"total":50}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        Assert.That(result, Does.Contain("Fallback"));
    }

    [Test]
    public async Task Enrich_MissingLookupKey_ThrowsWhenNoFallback()
    {
        var source = Substitute.For<IEnrichmentSource>();
        var options = new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "order.customerId",
            MergeTargetPath = "customer",
            FallbackOnFailure = false,
        };
        var enricher = new ContentEnricher(
            source, Options.Create(options),
            NullLogger<ContentEnricher>.Instance);

        var payload = """{"order":{"total":50}}""";
        Assert.ThrowsAsync<InvalidOperationException>(
            () => enricher.EnrichAsync(payload, Guid.NewGuid()));
    }

    [Test]
    public async Task Enrich_E2E_PublishEnrichedToMockEndpoint()
    {
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("C-100", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"name":"Alice"}"""));

        var enricher = CreateEnricher(source, "order.customerId", "customer");

        var payload = """{"order":{"customerId":"C-100","total":100}}""";
        var enriched = await enricher.EnrichAsync(payload, Guid.NewGuid());

        var envelope = IntegrationEnvelope<string>.Create(
            enriched, "EnricherService", "payload.enriched");
        await _output.PublishAsync(envelope, "enriched-topic", CancellationToken.None);

        _output.AssertReceivedOnTopic("enriched-topic", 1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Does.Contain("Alice"));
    }

    private static ContentEnricher CreateEnricher(
        IEnrichmentSource source, string lookupPath, string mergePath)
    {
        var options = new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = lookupPath,
            MergeTargetPath = mergePath,
        };
        return new ContentEnricher(
            source, Options.Create(options),
            NullLogger<ContentEnricher>.Instance);
    }
}
