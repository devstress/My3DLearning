// ============================================================================
// Tutorial 18 – Content Enricher (Exam)
// ============================================================================
// E2E challenges: deep nested merge path, enrichment with numeric lookup key,
// and multi-message enrichment batch verification via MockEndpoint.
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
public sealed class Exam
{
    [Test]
    public async Task Challenge1_DeepNestedMerge_EnrichesAtNestedPath()
    {
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("WH-1", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"location":"NYC","capacity":5000}"""));

        var options = new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/wh/{key}",
            LookupKeyPath = "shipment.warehouseId",
            MergeTargetPath = "shipment.warehouseDetails",
        };
        var enricher = new ContentEnricher(
            source, Options.Create(options),
            NullLogger<ContentEnricher>.Instance);

        var payload = """{"shipment":{"warehouseId":"WH-1","items":3}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        Assert.That(result, Does.Contain("NYC"));
        Assert.That(result, Does.Contain("5000"));
        Assert.That(result, Does.Contain("WH-1"));
    }

    [Test]
    public async Task Challenge2_NumericLookupKey_ExtractsCorrectly()
    {
        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("42", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"status":"active","plan":"enterprise"}"""));

        var options = new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/accounts/{key}",
            LookupKeyPath = "accountId",
            MergeTargetPath = "account",
        };
        var enricher = new ContentEnricher(
            source, Options.Create(options),
            NullLogger<ContentEnricher>.Instance);

        var payload = """{"accountId":42,"action":"upgrade"}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        Assert.That(result, Does.Contain("active"));
        Assert.That(result, Does.Contain("enterprise"));
    }

    [Test]
    public async Task Challenge3_BatchEnrichment_MultipleMessagesPublished()
    {
        await using var output = new MockEndpoint("exam-enricher");

        var source = Substitute.For<IEnrichmentSource>();
        source.FetchAsync("C-1", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"name":"Alice"}"""));
        source.FetchAsync("C-2", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"name":"Bob"}"""));
        source.FetchAsync("C-3", Arg.Any<CancellationToken>())
            .Returns(JsonNode.Parse("""{"name":"Charlie"}"""));

        var options = new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "customerId",
            MergeTargetPath = "customer",
        };
        var enricher = new ContentEnricher(
            source, Options.Create(options),
            NullLogger<ContentEnricher>.Instance);

        var payloads = new[]
        {
            """{"customerId":"C-1","amount":100}""",
            """{"customerId":"C-2","amount":200}""",
            """{"customerId":"C-3","amount":300}""",
        };

        foreach (var p in payloads)
        {
            var enriched = await enricher.EnrichAsync(p, Guid.NewGuid());
            var envelope = IntegrationEnvelope<string>.Create(
                enriched, "EnricherSvc", "enriched");
            await output.PublishAsync(envelope, "enriched-orders", CancellationToken.None);
        }

        output.AssertReceivedOnTopic("enriched-orders", 3);
        var all = output.GetAllReceived<string>();
        Assert.That(all[0].Payload, Does.Contain("Alice"));
        Assert.That(all[1].Payload, Does.Contain("Bob"));
        Assert.That(all[2].Payload, Does.Contain("Charlie"));
    }
}
