// ============================================================================
// Tutorial 50 – Best Practices (Lab)
// ============================================================================
// This lab exercises cross-cutting EIP best practices: envelope expiration,
// sanitization idempotency, tenant resolution, metadata, schema versioning,
// and message headers.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.MultiTenancy;
using EnterpriseIntegrationPlatform.Security;
using NUnit.Framework;

namespace TutorialLabs.Tutorial50;

[TestFixture]
public sealed class Lab
{
    // ── Envelope IsExpired For Past ExpiresAt ────────────────────────────────

    [Test]
    public void IntegrationEnvelope_IsExpired_TrueForPastDate()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Service", "event") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };

        Assert.That(envelope.IsExpired, Is.True);
    }

    // ── Envelope IsExpired For Future ExpiresAt ─────────────────────────────

    [Test]
    public void IntegrationEnvelope_IsExpired_FalseForFutureDate()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Service", "event") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        Assert.That(envelope.IsExpired, Is.False);
    }

    // ── InputSanitizer Idempotent ───────────────────────────────────────────

    [Test]
    public void InputSanitizer_Sanitize_IsIdempotent()
    {
        var sanitizer = new InputSanitizer();
        var input = "Hello <b>World</b>";

        var first = sanitizer.Sanitize(input);
        var second = sanitizer.Sanitize(first);

        Assert.That(second, Is.EqualTo(first));
    }

    // ── TenantResolver Handles Null TenantId ────────────────────────────────

    [Test]
    public void TenantResolver_NullTenantId_ReturnsAnonymous()
    {
        var resolver = new TenantResolver();
        var context = resolver.Resolve((string?)null);

        Assert.That(context.TenantId, Is.EqualTo(TenantContext.Anonymous.TenantId));
    }

    // ── MessageHeaders.ReplayId Exists ──────────────────────────────────────

    [Test]
    public void MessageHeaders_ReplayId_ConstantExists()
    {
        var replayId = MessageHeaders.ReplayId;

        Assert.That(replayId, Is.Not.Null.And.Not.Empty);
    }

    // ── Metadata Round-Trip ─────────────────────────────────────────────────

    [Test]
    public void IntegrationEnvelope_Metadata_RoundTrip()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Service", "event") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = "tenant-a",
                ["region"] = "us-east-1",
                ["priority"] = "high",
            },
        };

        Assert.That(envelope.Metadata["tenantId"], Is.EqualTo("tenant-a"));
        Assert.That(envelope.Metadata["region"], Is.EqualTo("us-east-1"));
        Assert.That(envelope.Metadata, Has.Count.EqualTo(3));
    }

    // ── SchemaVersion Defaults To 1.0 ───────────────────────────────────────

    [Test]
    public void IntegrationEnvelope_SchemaVersion_DefaultsTo1()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Service", "event");

        Assert.That(envelope.SchemaVersion, Is.EqualTo("1.0"));
    }
}
