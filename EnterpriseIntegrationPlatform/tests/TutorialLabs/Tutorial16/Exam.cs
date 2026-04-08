// ============================================================================
// Tutorial 16 – Transform Pipeline (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Transform Pipeline pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Regex replace step masks phone numbers in a pipeline
//   🟡 Intermediate — JsonPathFilter step retains only specified JSON paths
//   🔴 Advanced     — Multi-step pipeline transforms and publishes via MockEndpoint
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

namespace TutorialLabs.Tutorial16;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Regex replace step masks phone numbers ──────────────
    //
    // SCENARIO: A customer-support transcript contains phone numbers that
    //           must be masked before downstream processing. A RegexReplaceStep
    //           is configured to replace phone patterns with "***-****".
    //
    // WHAT YOU PROVE: A single RegexReplaceStep inside a pipeline correctly
    //                 masks all matching patterns and reports StepsApplied = 1.
    // ─────────────────────────────────────────────────────────────────────

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
    //
    // SCENARIO: An order payload arrives with order details, customer PII,
    //           and internal fields. Only the order ID and customer name
    //           should survive for the downstream analytics service.
    //
    // WHAT YOU PROVE: A JsonPathFilterStep inside a pipeline strips all
    //                 fields except the specified keep-paths, preserving
    //                 nested structure.
    // ─────────────────────────────────────────────────────────────────────

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
    //
    // SCENARIO: A raw message flows through three pipeline steps: regex
    //           replacement, upper-casing, and prefix tagging. The final
    //           transformed payload is published to a MockEndpoint and the
    //           received message must match exactly.
    //
    // WHAT YOU PROVE: A multi-step pipeline chains transforms in the correct
    //                 order and the result is faithfully published end-to-end.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_MultiStep_TransformAndPublish()
    {
        await using var output = new MockEndpoint("exam-transform");

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
