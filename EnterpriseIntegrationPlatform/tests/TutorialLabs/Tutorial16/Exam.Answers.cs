// ============================================================================
// Tutorial 16 – Transform Pipeline (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Regex replace step masks phone numbers in a pipeline
//   🟡 Intermediate — JsonPathFilter step retains only specified JSON paths
//   🔴 Advanced     — Multi-step pipeline transforms and publishes via MockEndpoint
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial16;

[TestFixture]
public sealed class ExamAnswers
{
    // ── 🟢 STARTER — Regex replace step masks phone numbers ──────────────

    [Test]
    public async Task Starter_RegexReplace_MasksPhoneNumbers()
    {
        var step = new RegexReplaceStep(@"\d{3}-\d{4}", "***-****");
        var options = Options.Create(new TransformOptions { Enabled = true });
        var pipeline = new TransformPipeline(
            new ITransformStep[] { step }, options,
            NullLogger<TransformPipeline>.Instance);

        var result = await pipeline.ExecuteAsync(
            "Call 555-1234 or 555-5678", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("Call ***-**** or ***-****"));
        Assert.That(result.StepsApplied, Is.EqualTo(1));
    }

    // ── 🟡 INTERMEDIATE — JsonPathFilter retains only specified paths ────

    [Test]
    public async Task Intermediate_JsonPathFilter_RetainsOnlySpecifiedPaths()
    {
        var step = new JsonPathFilterStep(new[] { "order.id", "customer.name" });
        var options = Options.Create(new TransformOptions { Enabled = true });
        var pipeline = new TransformPipeline(
            new ITransformStep[] { step }, options,
            NullLogger<TransformPipeline>.Instance);

        var payload = """{"order":{"id":"ORD-1","total":99.99},"customer":{"name":"Alice","email":"a@b.com"},"internal":"secret"}""";
        var result = await pipeline.ExecuteAsync(payload, "application/json");

        Assert.That(result.ContentType, Is.EqualTo("application/json"));
        Assert.That(result.Payload, Does.Contain("ORD-1"));
        Assert.That(result.Payload, Does.Contain("Alice"));
        Assert.That(result.Payload, Does.Not.Contain("secret"));
        Assert.That(result.Payload, Does.Not.Contain("a@b.com"));
    }

    // ── 🔴 ADVANCED — Multi-step transform and publish ─────────────────

    [Test]
    public async Task Advanced_MultiStep_TransformAndPublish()
    {
        await using var output = new MockEndpoint("exam-answers-transform");

        var steps = new ITransformStep[]
        {
            new RegexReplaceStep(@"\bfoo\b", "bar"),
            new UpperCaseStep(),
            new PrefixStep("[PROCESSED] "),
        };
        var options = Options.Create(new TransformOptions { Enabled = true });
        var pipeline = new TransformPipeline(
            steps, options, NullLogger<TransformPipeline>.Instance);

        var result = await pipeline.ExecuteAsync("the foo is here", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("[PROCESSED] THE BAR IS HERE"));
        Assert.That(result.StepsApplied, Is.EqualTo(3));

        var envelope = IntegrationEnvelope<string>.Create(
            result.Payload, "TransformSvc", "transform.done");
        await output.PublishAsync(envelope, "processed-topic", CancellationToken.None);

        output.AssertReceivedOnTopic("processed-topic", 1);
        var received = output.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("[PROCESSED] THE BAR IS HERE"));
    }
}
