// ============================================================================
// Tutorial 18 – Content Enricher (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Deep nested merge path enriches at a nested JSON location
//   🟡 Intermediate  — Numeric lookup key extracted and used for enrichment
//   🔴 Advanced      — Batch enrichment of multiple messages published via MockEndpoint
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using System.Text.Json.Nodes;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
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
        // TODO: Create a MockEnrichmentSource with appropriate configuration
        dynamic source = null!;

        // TODO: Create a ContentEnricherOptions with appropriate configuration
        dynamic options = null!;
        // TODO: Create a ContentEnricher with appropriate configuration
        dynamic enricher = null!;

        var payload = """{"shipment":{"warehouseId":"WH-1","items":3}}""";
        // TODO: var result = await enricher.EnrichAsync(...)
        dynamic result = null!;

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
        // TODO: Create a MockEnrichmentSource with appropriate configuration
        dynamic source = null!;

        // TODO: Create a ContentEnricherOptions with appropriate configuration
        dynamic options = null!;
        // TODO: Create a ContentEnricher with appropriate configuration
        dynamic enricher = null!;

        var payload = """{"accountId":42,"action":"upgrade"}""";
        // TODO: var result = await enricher.EnrichAsync(...)
        dynamic result = null!;

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

        // TODO: Create a MockEnrichmentSource with appropriate configuration
        dynamic source = null!;

        // TODO: Create a ContentEnricherOptions with appropriate configuration
        dynamic options = null!;
        // TODO: Create a ContentEnricher with appropriate configuration
        dynamic enricher = null!;

        var payloads = new[]
        {
            """{"customerId":"C-1","amount":100}""",
            """{"customerId":"C-2","amount":200}""",
            """{"customerId":"C-3","amount":300}""",
        };

        foreach (var p in payloads)
        {
            // TODO: var enriched = await enricher.EnrichAsync(...)
            dynamic enriched = null!;
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await output.PublishAsync(...)
        }

        output.AssertReceivedOnTopic("enriched-orders", 3);
        var all = output.GetAllReceived<string>();
        Assert.That(all[0].Payload, Does.Contain("Alice"));
        Assert.That(all[1].Payload, Does.Contain("Bob"));
        Assert.That(all[2].Payload, Does.Contain("Charlie"));
    }
}
#endif
