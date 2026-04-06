// ============================================================================
// Tutorial 33 – Security (Exam)
// ============================================================================
// EIP Pattern: Security Patterns
// E2E: Full sanitize pipeline, byte[] payload guard, and combined
//      sanitizer + size guard E2E flow with MockEndpoint.
// ============================================================================
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Security;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial33;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_FullSanitizePipeline_PublishesCleanMessages()
    {
        await using var output = new MockEndpoint("exam-sanitize");
        var sanitizer = new InputSanitizer();

        var inputs = new[]
        {
            "normal text",
            "<script>alert('xss')</script>injected",
            "hello\r\nworld",
        };

        foreach (var raw in inputs)
        {
            var clean = sanitizer.Sanitize(raw);
            var envelope = IntegrationEnvelope<string>.Create(clean, "pipeline", "Sanitized");
            await output.PublishAsync(envelope, "clean-output", default);
        }

        output.AssertReceivedOnTopic("clean-output", 3);
        var all = output.GetAllReceived<string>("clean-output");
        foreach (var env in all)
        {
            Assert.That(sanitizer.IsClean(env.Payload), Is.True);
        }
    }

    [Test]
    public async Task Challenge2_ByteArrayPayloadGuard()
    {
        await using var output = new MockEndpoint("exam-bytes");
        var guard = new PayloadSizeGuard(Options.Create(
            new PayloadSizeOptions { MaxPayloadBytes = 16 }));

        var smallBytes = new byte[10];
        Assert.DoesNotThrow(() => guard.Enforce(smallBytes));

        var largeBytes = new byte[20];
        var ex = Assert.Throws<PayloadTooLargeException>(
            () => guard.Enforce(largeBytes));

        Assert.That(ex!.ActualBytes, Is.EqualTo(20));
        Assert.That(ex.MaxBytes, Is.EqualTo(16));

        var envelope = IntegrationEnvelope<string>.Create(
            "byte-guard-verified", "guard", "SizeCheck");
        await output.PublishAsync(envelope, "guard-results", default);
        output.AssertReceivedOnTopic("guard-results", 1);
    }

    [Test]
    public async Task Challenge3_CombinedSanitizerAndGuard_E2E()
    {
        await using var output = new MockEndpoint("exam-combined");
        var sanitizer = new InputSanitizer();
        var guard = new PayloadSizeGuard(Options.Create(
            new PayloadSizeOptions { MaxPayloadBytes = 4096 }));

        var raw = "Order: <script>steal()</script> amount=100 OR 1=1";
        var clean = sanitizer.Sanitize(raw);
        guard.Enforce(clean);
        Assert.That(sanitizer.IsClean(clean), Is.True);

        var envelope = IntegrationEnvelope<string>.Create(clean, "security", "Verified");
        await output.PublishAsync(envelope, "verified-output", default);
        output.AssertReceivedOnTopic("verified-output", 1);

        var oversized = new string('X', 5000);
        Assert.Throws<PayloadTooLargeException>(() => guard.Enforce(oversized));
        output.AssertReceivedCount(1);
    }
}
