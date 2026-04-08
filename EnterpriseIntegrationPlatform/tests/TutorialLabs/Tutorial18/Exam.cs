// ============================================================================
// Tutorial 18 – Content Enricher (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Content Enricher pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Deep nested merge path enriches at a nested JSON location
//   🟡 Intermediate — Numeric lookup key extracted and used for enrichment
//   🔴 Advanced     — Batch enrichment of multiple messages published via MockEndpoint
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint / MockEnrichmentSource
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
public sealed class Exam
{
    // ── 🟢 STARTER — Deep nested merge at a nested path ─────────────────
    //
    // SCENARIO: A shipment payload contains a warehouseId nested under
    //           "shipment". The enricher must fetch warehouse details and
    //           merge them at "shipment.warehouseDetails".
    //
    // WHAT YOU PROVE: The enricher correctly extracts a nested lookup key,
    //                 fetches external data, and merges at a deep target path.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_DeepNestedMerge_EnrichesAtNestedPath()
    {
        var source = new MockEnrichmentSource()
            .WithData("WH-1", """{"location":"NYC","capacity":5000}""");

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

    // ── 🟡 INTERMEDIATE — Numeric lookup key extraction ─────────────────
    //
    // SCENARIO: An account payload has a numeric accountId (42). The enricher
    //           must extract the numeric value as a string key and fetch
    //           the associated account details.
    //
    // WHAT YOU PROVE: The enricher handles numeric JSON values as lookup
    //                 keys by converting them to string for the fetch call.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_NumericLookupKey_ExtractsCorrectly()
    {
        var source = new MockEnrichmentSource()
            .WithData("42", """{"status":"active","plan":"enterprise"}""");

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

    // ── 🔴 ADVANCED — Batch enrichment of multiple messages ─────────────
    //
    // SCENARIO: Three order messages arrive, each with a different customerId.
    //           Each must be enriched with the customer's name from an external
    //           source and published to a shared topic. The batch count and
    //           per-message content must be verified.
    //
    // WHAT YOU PROVE: The enricher handles a batch of messages end-to-end,
    //                 enriching each with the correct external data and
    //                 publishing all results faithfully.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_BatchEnrichment_MultipleMessagesPublished()
    {
        await using var output = new MockEndpoint("exam-enricher");

        var source = new MockEnrichmentSource()
            .WithData("C-1", """{"name":"Alice"}""")
            .WithData("C-2", """{"name":"Bob"}""")
            .WithData("C-3", """{"name":"Charlie"}""");

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
