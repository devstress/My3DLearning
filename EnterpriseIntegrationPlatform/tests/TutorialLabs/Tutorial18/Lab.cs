// ============================================================================
// Tutorial 18 – Content Enricher (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Content Enricher pattern
//          augments messages with external data by merging fetched fields
//          without overwriting existing payload.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Enrich_MergesExternalData                      — merge external data at a target path
//   2. Enrich_NestedLookup_ExtractsCorrectKey         — nested lookup key path extracts correct value
//   3. Enrich_SourceReturnsNull_UsesFallback          — source returns null — fallback value merged
//   4. Enrich_MissingLookupKey_FallsBack              — missing lookup key with fallback returns gracefully
//   5. Enrich_MissingLookupKey_ThrowsWhenNoFallback   — missing lookup key without fallback throws
//   6. Enrich_E2E_PublishEnrichedToNatsEndpoint       — end-to-end enrich and publish via real NATS
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire) / MockEnrichmentSource
// ============================================================================

using System.Text.Json.Nodes;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial18;

[TestFixture]
public sealed class Lab
{
    // ── 1. Enrichment & Lookup ───────────────────────────────────────

    [Test]
    public async Task Enrich_MergesExternalData()
    {
        var source = new MockEnrichmentSource()
            .WithData("C-100", """{"name":"Alice","tier":"Gold"}""");

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
        var source = new MockEnrichmentSource()
            .WithData("P-200", """{"sku":"Widget","warehouse":"WH-1"}""");

        var enricher = CreateEnricher(source, "line.productId", "product");

        var payload = """{"line":{"productId":"P-200","qty":5}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        Assert.That(result, Does.Contain("Widget"));
        Assert.That(result, Does.Contain("WH-1"));
    }


    // ── 2. Fallback Behaviour ────────────────────────────────────────

    [Test]
    public async Task Enrich_SourceReturnsNull_UsesFallback()
    {
        var source = new MockEnrichmentSource()
            .ReturnsNullForUnknown();

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
        var source = new MockEnrichmentSource()
            .ReturnsNullForUnknown();
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
        var source = new MockEnrichmentSource()
            .ReturnsNullForUnknown();
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


    // ── 3. End-to-End Integration (Real NATS) ────────────────────────

    [Test]
    public async Task Enrich_E2E_PublishEnrichedToNatsEndpoint()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t18-e2e");
        var topic = AspireFixture.UniqueTopic("t18-enriched");

        var source = new MockEnrichmentSource()
            .WithData("C-100", """{"name":"Alice"}""");

        var enricher = CreateEnricher(source, "order.customerId", "customer");

        var payload = """{"order":{"customerId":"C-100","total":100}}""";
        var enriched = await enricher.EnrichAsync(payload, Guid.NewGuid());

        var envelope = IntegrationEnvelope<string>.Create(
            enriched, "EnricherService", "payload.enriched");
        await nats.PublishAsync(envelope, topic, CancellationToken.None);

        nats.AssertReceivedOnTopic(topic, 1);
        var received = nats.GetReceived<string>();
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
