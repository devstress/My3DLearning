// ============================================================================
// Tutorial 18 – Content Enricher (Lab)
// ============================================================================
// This lab exercises the ContentEnricher — the pattern that augments a
// message payload with data fetched from an external source. You will
// mock IEnrichmentSource to return supplementary data and verify lookup-
// key extraction, data merging, fallback behaviour, and missing-key paths.
// ============================================================================

using System.Text.Json;
using System.Text.Json.Nodes;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial18;

[TestFixture]
public sealed class Lab
{
    // ── Basic Enrichment ────────────────────────────────────────────────────

    [Test]
    public async Task Enrich_MergesExternalDataAtTargetPath()
    {
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("CUST-1", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"name":"Alice","tier":"Gold"}"""));

        var options = Options.Create(new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/customers/{key}",
            LookupKeyPath = "customerId",
            MergeTargetPath = "customer",
        });

        var enricher = new ContentEnricher(
            source, options, NullLogger<ContentEnricher>.Instance);

        var payload = """{"orderId":"ORD-1","customerId":"CUST-1","total":100}""";

        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("orderId").GetString(), Is.EqualTo("ORD-1"));
        Assert.That(
            doc.RootElement.GetProperty("customer").GetProperty("name").GetString(),
            Is.EqualTo("Alice"));
        Assert.That(
            doc.RootElement.GetProperty("customer").GetProperty("tier").GetString(),
            Is.EqualTo("Gold"));
    }

    // ── Nested Lookup Key ───────────────────────────────────────────────────

    [Test]
    public async Task Enrich_NestedLookupKeyPath_ExtractsCorrectValue()
    {
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("ADDR-7", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"city":"Seattle","zip":"98101"}"""));

        var options = Options.Create(new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/addresses/{key}",
            LookupKeyPath = "order.addressId",
            MergeTargetPath = "shippingAddress",
        });

        var enricher = new ContentEnricher(
            source, options, NullLogger<ContentEnricher>.Instance);

        var payload = """{"order":{"id":"ORD-2","addressId":"ADDR-7"}}""";

        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(
            doc.RootElement.GetProperty("shippingAddress").GetProperty("city").GetString(),
            Is.EqualTo("Seattle"));
    }

    // ── Missing Lookup Key — Fallback ───────────────────────────────────────

    [Test]
    public async Task Enrich_MissingLookupKey_FallbackEnabled_ReturnsOriginal()
    {
        var source = Substitute.For<IEnrichmentSource>();

        var options = Options.Create(new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "nonExistentField",
            MergeTargetPath = "extra",
            FallbackOnFailure = true,
        });

        var enricher = new ContentEnricher(
            source, options, NullLogger<ContentEnricher>.Instance);

        var payload = """{"id":"X"}""";

        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("id").GetString(), Is.EqualTo("X"));
        await source.DidNotReceive().FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Missing Lookup Key — No Fallback ────────────────────────────────────

    [Test]
    public void Enrich_MissingLookupKey_NoFallback_Throws()
    {
        var source = Substitute.For<IEnrichmentSource>();

        var options = Options.Create(new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "missingKey",
            MergeTargetPath = "extra",
            FallbackOnFailure = false,
        });

        var enricher = new ContentEnricher(
            source, options, NullLogger<ContentEnricher>.Instance);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => enricher.EnrichAsync("""{"id":1}""", Guid.NewGuid()));
    }

    // ── Source Returns Null — Fallback Value ────────────────────────────────

    [Test]
    public async Task Enrich_SourceReturnsNull_FallbackValue_MergesFallback()
    {
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("KEY-1", Arg.Any<CancellationToken>())
            .Returns((JsonNode?)null);

        var options = Options.Create(new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "key",
            MergeTargetPath = "extra",
            FallbackOnFailure = true,
            FallbackValue = """{"status":"unknown"}""",
        });

        var enricher = new ContentEnricher(
            source, options, NullLogger<ContentEnricher>.Instance);

        var payload = """{"key":"KEY-1"}""";

        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(
            doc.RootElement.GetProperty("extra").GetProperty("status").GetString(),
            Is.EqualTo("unknown"));
    }

    // ── Enrichment Preserves Existing Fields ────────────────────────────────

    [Test]
    public async Task Enrich_PreservesAllExistingPayloadFields()
    {
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("C-1", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"loyalty":true}"""));

        var options = Options.Create(new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "cid",
            MergeTargetPath = "loyalty",
        });

        var enricher = new ContentEnricher(
            source, options, NullLogger<ContentEnricher>.Instance);

        var payload = """{"cid":"C-1","amount":50,"currency":"USD"}""";

        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("cid").GetString(), Is.EqualTo("C-1"));
        Assert.That(doc.RootElement.GetProperty("amount").GetInt32(), Is.EqualTo(50));
        Assert.That(doc.RootElement.GetProperty("currency").GetString(), Is.EqualTo("USD"));
    }
}
