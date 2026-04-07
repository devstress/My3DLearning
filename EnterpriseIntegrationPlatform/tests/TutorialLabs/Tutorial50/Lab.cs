// ============================================================================
// Tutorial 50 – Best Practices (Lab)
// ============================================================================
// EIP Pattern: Cross-cutting best practices integration.
// E2E: Combine envelope expiration, sanitization, tenancy, and metadata
// with NatsBrokerEndpoint (real NATS JetStream via Aspire) to demonstrate
// production-ready message flows.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.MultiTenancy;
using EnterpriseIntegrationPlatform.Security;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial50;

[TestFixture]
public sealed class Lab
{
    // ── 1. Message Expiration ────────────────────────────────────────

    [Test]
    public async Task ExpiredMessage_NotPublished()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t50-expired");
        var topic = AspireFixture.UniqueTopic("t50-active");

        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "event") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };

        if (!envelope.IsExpired)
            await nats.PublishAsync(envelope, topic, default);

        Assert.That(envelope.IsExpired, Is.True);
        nats.AssertNoneReceived();
    }

    [Test]
    public async Task ValidMessage_Published()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t50-valid");
        var topic = AspireFixture.UniqueTopic("t50-active");

        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "event") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        if (!envelope.IsExpired)
            await nats.PublishAsync(envelope, topic, default);

        Assert.That(envelope.IsExpired, Is.False);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 2. Security & Multi-Tenancy ──────────────────────────────────

    [Test]
    public void InputSanitizer_Idempotent()
    {
        var sanitizer = new InputSanitizer();
        var input = "Hello <b>World</b>";
        var first = sanitizer.Sanitize(input);
        var second = sanitizer.Sanitize(first);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void TenantResolver_NullTenantId_ReturnsAnonymous()
    {
        var resolver = new TenantResolver();
        var context = resolver.Resolve((string?)null);

        Assert.That(context.TenantId, Is.EqualTo(TenantContext.Anonymous.TenantId));
    }


    // ── 3. Metadata & Schema ─────────────────────────────────────────

    [Test]
    public void MessageHeaders_ReplayId_ConstantExists()
    {
        Assert.That(MessageHeaders.ReplayId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task Metadata_RoundTrip_PublishedWithEnvelope()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t50-metadata");
        var topic = AspireFixture.UniqueTopic("t50-metadata");

        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "event") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = "tenant-a",
                ["region"] = "us-east-1",
                ["priority"] = "high",
            },
        };

        await nats.PublishAsync(envelope, topic, default);

        nats.AssertReceivedOnTopic(topic, 1);
        var received = nats.GetReceived<string>();
        Assert.That(received.Metadata["tenantId"], Is.EqualTo("tenant-a"));
        Assert.That(received.Metadata, Has.Count.EqualTo(3));
    }

    [Test]
    public void SchemaVersion_DefaultsTo1()
    {
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "event");
        Assert.That(envelope.SchemaVersion, Is.EqualTo("1.0"));
    }
}
