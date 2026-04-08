// ============================================================================
// Tutorial 19 – Content Filter (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Content Filter pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — JsonPathFilterStep filters fields inside a transform pipeline
//   🟡 Intermediate — Deeply nested filter extracts correct leaf values
//   🔴 Advanced     — Batch filter of multiple messages published via MockEndpoint
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial19;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — JsonPathFilterStep filters in a pipeline ───────────
    //
    // SCENARIO: An order event payload contains order details, customer PII,
    //           and internal fields. A JsonPathFilterStep in a transform
    //           pipeline must retain only order.id and customer.name.
    //
    // WHAT YOU PROVE: A JsonPathFilterStep works correctly as a pipeline
    //                 step, stripping all fields except the keep-paths.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_JsonPathFilterStep_FiltersInPipeline()
    {
        var step = new JsonPathFilterStep(new[] { "order.id", "customer.name" });
        var options = Options.Create(new TransformOptions { Enabled = true });
        var pipeline = new TransformPipeline(
            new ITransformStep[] { step }, options,
            NullLogger<TransformPipeline>.Instance);

        var payload = """{"order":{"id":"ORD-1","total":99},"customer":{"name":"Alice","email":"a@b.com"},"internal":"x"}""";
        var result = await pipeline.ExecuteAsync(payload, "application/json");

        Assert.That(result.Payload, Does.Contain("ORD-1"));
        Assert.That(result.Payload, Does.Contain("Alice"));
        Assert.That(result.Payload, Does.Not.Contain("internal"));
        Assert.That(result.StepsApplied, Is.EqualTo(1));
    }

    // ── 🟡 INTERMEDIATE — Deeply nested filter extraction ───────────────
    //
    // SCENARIO: A configuration payload has data nested four levels deep.
    //           Only the leaf value at "level1.level2.level3.target" should
    //           survive filtering; all sibling and ancestor fields are removed.
    //
    // WHAT YOU PROVE: The content filter correctly navigates and extracts
    //                 deeply nested paths while stripping everything else.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_DeeplyNestedFilter_ExtractsCorrectly()
    {
        var filter = new ContentFilter(NullLogger<ContentFilter>.Instance);

        var payload = """{"level1":{"level2":{"level3":{"target":"found","other":"skip"}},"sibling":"also-skip"}}""";
        var result = await filter.FilterAsync(payload, new[] { "level1.level2.level3.target" });

        Assert.That(result, Does.Contain("found"));
        Assert.That(result, Does.Not.Contain("skip"));
    }

    // ── 🔴 ADVANCED — Batch filter of multiple messages ─────────────────
    //
    // SCENARIO: Three employee records arrive containing user, role, and
    //           salary fields. Only user and role should survive filtering.
    //           All filtered messages are published to a "safe-data" topic
    //           and the batch count and content verified.
    //
    // WHAT YOU PROVE: The content filter handles a batch of messages
    //                 end-to-end, stripping sensitive fields and publishing
    //                 all results faithfully.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_BatchFilter_MultipleMessagesPublished()
    {
        await using var output = new MockEndpoint("exam-filter");
        var filter = new ContentFilter(NullLogger<ContentFilter>.Instance);

        var payloads = new[]
        {
            """{"user":"Alice","role":"admin","salary":100000}""",
            """{"user":"Bob","role":"viewer","salary":60000}""",
            """{"user":"Charlie","role":"editor","salary":80000}""",
        };

        foreach (var p in payloads)
        {
            var filtered = await filter.FilterAsync(p, new[] { "user", "role" });
            var envelope = IntegrationEnvelope<string>.Create(
                filtered, "FilterSvc", "filtered");
            await output.PublishAsync(envelope, "safe-data", CancellationToken.None);
        }

        output.AssertReceivedOnTopic("safe-data", 3);
        var all = output.GetAllReceived<string>();
        Assert.That(all[0].Payload, Does.Not.Contain("100000"));
        Assert.That(all[1].Payload, Does.Not.Contain("60000"));
        Assert.That(all[2].Payload, Does.Contain("Charlie"));
    }
}
