// ============================================================================
// Tutorial 16 – Transform Pipeline (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
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

#if EXAM_STUDENT
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
        var options = Options.Create(new TransformOptions { Enabled = true });

        // TODO: Create a RegexReplaceStep matching phone patterns (\d{3}-\d{4})
        //       replacing with "***-****", then build a TransformPipeline with it.
        var pipeline = new TransformPipeline(
            new ITransformStep[] { /* TODO: your RegexReplaceStep here */ },
            options, NullLogger<TransformPipeline>.Instance);

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
        var options = Options.Create(new TransformOptions { Enabled = true });

        // TODO: Create a JsonPathFilterStep that keeps only "order.id" and "customer.name",
        //       then build a TransformPipeline with it.
        var pipeline = new TransformPipeline(
            new ITransformStep[] { /* TODO: your JsonPathFilterStep here */ },
            options, NullLogger<TransformPipeline>.Instance);

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

        var options = Options.Create(new TransformOptions { Enabled = true });

        // TODO: Create an array of 3 ITransformStep instances:
        //   1. RegexReplaceStep that replaces whole-word "foo" (\bfoo\b) with "bar"
        //   2. UpperCaseStep
        //   3. PrefixStep with prefix "[PROCESSED] "
        //   Then build a TransformPipeline with them.
        var pipeline = new TransformPipeline(
            new ITransformStep[]
            {
                /* TODO: your 3 steps here */
            },
            options, NullLogger<TransformPipeline>.Instance);

        var result = await pipeline.ExecuteAsync("the foo is here", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("[PROCESSED] THE BAR IS HERE"));
        Assert.That(result.StepsApplied, Is.EqualTo(3));

        // TODO: Create an IntegrationEnvelope<string>.Create() with the result.Payload,
        //       source "TransformSvc", message type "transform.done".
        //       Then call output.PublishAsync(envelope, "processed-topic", CancellationToken.None)
        IntegrationEnvelope<string> envelope = null!; // ← replace with IntegrationEnvelope<string>.Create(...)
        await output.PublishAsync(envelope, "processed-topic", CancellationToken.None);

        output.AssertReceivedOnTopic("processed-topic", 1);
        var received = output.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("[PROCESSED] THE BAR IS HERE"));
    }
}
#endif
