// ============================================================================
// Tutorial 33 – Security (Lab)
// ============================================================================
// EIP Pattern: Security Patterns (Input Sanitization + Payload Size Guard)
// E2E: InputSanitizer sanitize/detect, PayloadSizeGuard enforce,
//      MockEndpoint for publishing sanitized messages.
// ============================================================================
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Security;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial33;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("security-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    [Test]
    public async Task Sanitizer_RemovesScriptTags()
    {
        var sanitizer = new InputSanitizer();
        var input = "Hello <script>alert('xss')</script> World";
        var clean = sanitizer.Sanitize(input);

        Assert.That(clean, Does.Not.Contain("<script>"));
        Assert.That(clean, Does.Contain("Hello"));

        var envelope = IntegrationEnvelope<string>.Create(clean, "sanitizer", "Sanitized");
        await _output.PublishAsync(envelope, "sanitized-messages", default);
        _output.AssertReceivedOnTopic("sanitized-messages", 1);
    }

    [Test]
    public async Task Sanitizer_RemovesSqlInjection()
    {
        var sanitizer = new InputSanitizer();
        var input = "'; DROP TABLE users";
        var clean = sanitizer.Sanitize(input);

        Assert.That(clean, Does.Not.Contain("DROP TABLE"));
        await Task.CompletedTask;
    }

    [Test]
    public async Task IsClean_DetectsDangerousInput()
    {
        var sanitizer = new InputSanitizer();

        Assert.That(sanitizer.IsClean("safe text"), Is.True);
        Assert.That(sanitizer.IsClean("has\nnewline"), Is.False);
        Assert.That(sanitizer.IsClean("<script>xss</script>"), Is.False);
        await Task.CompletedTask;
    }

    [Test]
    public async Task PayloadSizeGuard_AllowsUnderLimit()
    {
        var guard = new PayloadSizeGuard(Options.Create(
            new PayloadSizeOptions { MaxPayloadBytes = 1024 }));

        Assert.DoesNotThrow(() => guard.Enforce("small payload"));
        await Task.CompletedTask;
    }

    [Test]
    public async Task PayloadSizeGuard_RejectsOverLimit()
    {
        var guard = new PayloadSizeGuard(Options.Create(
            new PayloadSizeOptions { MaxPayloadBytes = 10 }));

        var ex = Assert.Throws<PayloadTooLargeException>(
            () => guard.Enforce("this is way too large for limit"));

        Assert.That(ex!.MaxBytes, Is.EqualTo(10));
        Assert.That(ex.ActualBytes, Is.GreaterThan(10));
        await Task.CompletedTask;
    }

    [Test]
    public async Task SanitizedMessage_PublishedToMockEndpoint()
    {
        var sanitizer = new InputSanitizer();
        var guard = new PayloadSizeGuard(Options.Create(
            new PayloadSizeOptions { MaxPayloadBytes = 4096 }));

        var raw = "Hello <script>alert('x')</script> World";
        var clean = sanitizer.Sanitize(raw);
        guard.Enforce(clean);

        var envelope = IntegrationEnvelope<string>.Create(clean, "pipeline", "SafeMessage");
        await _output.PublishAsync(envelope, "safe-messages", default);
        _output.AssertReceivedOnTopic("safe-messages", 1);

        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Does.Not.Contain("<script>"));
    }
}
