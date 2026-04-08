// ============================================================================
// Tutorial 19 – Content Filter (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — JsonPathFilterStep filters fields inside a transform pipeline
//   🟡 Intermediate — Deeply nested filter extracts correct leaf values
//   🔴 Advanced     — Batch filter of multiple messages published via MockEndpoint
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial19;

[TestFixture]
public sealed class ExamAnswers
{
    // ── 🟢 STARTER — JsonPathFilterStep filters in a pipeline ───────────

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
