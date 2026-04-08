// ============================================================================
// Tutorial 33 – Security (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — full sanitize pipeline_ publishes clean messages
//   🟡 Intermediate  — byte array payload guard
//   🔴 Advanced      — combined sanitizer and guard_ e2 e
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Security;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial33;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_FullSanitizePipeline_PublishesCleanMessages()
    {
        await using var output = new MockEndpoint("exam-sanitize");
        // TODO: Create a InputSanitizer with appropriate configuration
        dynamic sanitizer = null!;

        var inputs = new[]
        {
            "normal text",
            "<script>alert('xss')</script>injected",
            "hello\r\nworld",
        };

        foreach (var raw in inputs)
        {
            var clean = sanitizer.Sanitize(raw);
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await output.PublishAsync(...)
        }

        output.AssertReceivedOnTopic("clean-output", 3);
        var all = output.GetAllReceived<string>("clean-output");
        foreach (var env in all)
        {
            Assert.That(sanitizer.IsClean(env.Payload), Is.True);
        }
    }

    [Test]
    public async Task Intermediate_ByteArrayPayloadGuard()
    {
        await using var output = new MockEndpoint("exam-bytes");
        // TODO: Create a PayloadSizeGuard with appropriate configuration
        dynamic guard = null!;

        // TODO: Create a byte with appropriate configuration
        dynamic smallBytes = null!;
        Assert.DoesNotThrow(() => guard.Enforce(smallBytes));

        // TODO: Create a byte with appropriate configuration
        dynamic largeBytes = null!;
        var ex = Assert.Throws<PayloadTooLargeException>(
            () => guard.Enforce(largeBytes));

        Assert.That(ex!.ActualBytes, Is.EqualTo(20));
        Assert.That(ex.MaxBytes, Is.EqualTo(16));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("guard-results", 1);
    }

    [Test]
    public async Task Advanced_CombinedSanitizerAndGuard_E2E()
    {
        await using var output = new MockEndpoint("exam-combined");
        // TODO: Create a InputSanitizer with appropriate configuration
        dynamic sanitizer = null!;
        // TODO: Create a PayloadSizeGuard with appropriate configuration
        dynamic guard = null!;

        var raw = "Order: <script>steal()</script> amount=100 OR 1=1";
        var clean = sanitizer.Sanitize(raw);
        guard.Enforce(clean);
        Assert.That(sanitizer.IsClean(clean), Is.True);

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("verified-output", 1);

        // TODO: Create a string with appropriate configuration
        dynamic oversized = null!;
        Assert.Throws<PayloadTooLargeException>(() => guard.Enforce(oversized));
        output.AssertReceivedCount(1);
    }
}
#endif
