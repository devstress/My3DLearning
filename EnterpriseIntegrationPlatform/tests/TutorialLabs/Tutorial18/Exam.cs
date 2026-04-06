// ============================================================================
// Tutorial 18 – Content Enricher (Exam)
// ============================================================================
// Coding challenges: enrich an order with customer details, test fallback
// on enrichment failure, and merge data at a nested target path.
// ============================================================================

using System.Text.Json;
using System.Text.Json.Nodes;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace TutorialLabs.Tutorial18;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Full Order Enrichment ──────────────────────────────────

    [Test]
    public async Task Challenge1_EnrichOrder_MergesCustomerDetails()
    {
        // An order payload contains a customerId. The enricher should fetch
        // the customer details and merge them under the "customer" property.
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("C-42", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse(
                """{"name":"Bob","email":"bob@example.com","tier":"Platinum"}"""));

        var options = Options.Create(new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/customers/{key}",
            LookupKeyPath = "customerId",
            MergeTargetPath = "customer",
        });

        var enricher = new ContentEnricher(
            source, options, NullLogger<ContentEnricher>.Instance);

        var payload = """{"orderId":"ORD-99","customerId":"C-42","items":3,"total":450}""";

        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        // Original fields preserved.
        Assert.That(root.GetProperty("orderId").GetString(), Is.EqualTo("ORD-99"));
        Assert.That(root.GetProperty("items").GetInt32(), Is.EqualTo(3));
        Assert.That(root.GetProperty("total").GetInt32(), Is.EqualTo(450));

        // Enriched customer data merged.
        var customer = root.GetProperty("customer");
        Assert.That(customer.GetProperty("name").GetString(), Is.EqualTo("Bob"));
        Assert.That(customer.GetProperty("email").GetString(), Is.EqualTo("bob@example.com"));
        Assert.That(customer.GetProperty("tier").GetString(), Is.EqualTo("Platinum"));
    }

    // ── Challenge 2: Fallback on Source Failure ─────────────────────────────

    [Test]
    public async Task Challenge2_SourceThrows_FallbackEnabled_UsesFallbackValue()
    {
        // When the enrichment source throws an exception but FallbackOnFailure
        // is enabled, the enricher should merge the configured FallbackValue.
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        var options = Options.Create(new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "userId",
            MergeTargetPath = "profile",
            FallbackOnFailure = true,
            FallbackValue = """{"name":"Unknown","status":"fallback"}""",
        });

        var enricher = new ContentEnricher(
            source, options, NullLogger<ContentEnricher>.Instance);

        var payload = """{"userId":"U-1","action":"login"}""";

        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(
            doc.RootElement.GetProperty("profile").GetProperty("status").GetString(),
            Is.EqualTo("fallback"));
        Assert.That(doc.RootElement.GetProperty("action").GetString(), Is.EqualTo("login"));
    }

    // ── Challenge 3: Nested Merge Target Path ───────────────────────────────

    [Test]
    public async Task Challenge3_NestedMergeTarget_CreatesIntermediateObjects()
    {
        // The merge target path can be a nested path like "metadata.enrichment".
        // The enricher should create intermediate JSON objects as needed.
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("REF-5", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"source":"external-api","timestamp":"2024-01-01"}"""));

        var options = Options.Create(new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "refId",
            MergeTargetPath = "metadata.enrichment",
        });

        var enricher = new ContentEnricher(
            source, options, NullLogger<ContentEnricher>.Instance);

        var payload = """{"refId":"REF-5","data":"important"}""";

        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        var enrichment = doc.RootElement
            .GetProperty("metadata")
            .GetProperty("enrichment");
        Assert.That(enrichment.GetProperty("source").GetString(), Is.EqualTo("external-api"));
        Assert.That(enrichment.GetProperty("timestamp").GetString(), Is.EqualTo("2024-01-01"));
    }
}
