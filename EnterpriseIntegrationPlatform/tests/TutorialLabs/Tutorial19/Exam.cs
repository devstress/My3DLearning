// ============================================================================
// Tutorial 19 – Content Filter (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — JsonPathFilterStep filters fields inside a transform pipeline
//   🟡 Intermediate  — Deeply nested filter extracts correct leaf values
//   🔴 Advanced      — Batch filter of multiple messages published via MockEndpoint
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
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
        // TODO: Create a JsonPathFilterStep with appropriate configuration
        dynamic step = null!;
        // TODO: var options = Options.Create(...)
        dynamic options = null!;
        // TODO: Create a TransformPipeline with appropriate configuration
        dynamic pipeline = null!;

        var payload = """{"order":{"id":"ORD-1","total":99},"customer":{"name":"Alice","email":"a@b.com"},"internal":"x"}""";
        // TODO: var result = await pipeline.ExecuteAsync(...)
        dynamic result = null!;

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
        // TODO: Create a ContentFilter with appropriate configuration
        dynamic filter = null!;

        var payload = """{"level1":{"level2":{"level3":{"target":"found","other":"skip"}},"sibling":"also-skip"}}""";
        // TODO: var result = await filter.FilterAsync(...)
        dynamic result = null!;

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
        // TODO: Create a ContentFilter with appropriate configuration
        dynamic filter = null!;

        var payloads = new[]
        {
            """{"user":"Alice","role":"admin","salary":100000}""",
            """{"user":"Bob","role":"viewer","salary":60000}""",
            """{"user":"Charlie","role":"editor","salary":80000}""",
        };

        foreach (var p in payloads)
        {
            // TODO: var filtered = await filter.FilterAsync(...)
            dynamic filtered = null!;
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await output.PublishAsync(...)
        }

        output.AssertReceivedOnTopic("safe-data", 3);
        var all = output.GetAllReceived<string>();
        Assert.That(all[0].Payload, Does.Not.Contain("100000"));
        Assert.That(all[1].Payload, Does.Not.Contain("60000"));
        Assert.That(all[2].Payload, Does.Contain("Charlie"));
    }
}
#endif
