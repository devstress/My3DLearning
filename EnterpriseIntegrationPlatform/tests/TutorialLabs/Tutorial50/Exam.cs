// ============================================================================
// Tutorial 50 – Best Practices (Exam)
// ============================================================================
// Coding challenges: end-to-end envelope with security and tenancy,
// expiration and priority scenarios, cross-cutting concern integration.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.MultiTenancy;
using EnterpriseIntegrationPlatform.Security;
using NUnit.Framework;

namespace TutorialLabs.Tutorial50;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: End-to-End Envelope + Security + Tenancy ────────────────

    [Test]
    public void Challenge1_EndToEnd_EnvelopeSecurityTenancy()
    {
        // Create an envelope with metadata
        var envelope = IntegrationEnvelope<string>.Create(
            "<script>alert('xss')</script> Order data", "OrderService", "order.created") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = "premium-corp",
                ["region"] = "eu-west-1",
            },
        };

        // Sanitize the payload
        var sanitizer = new InputSanitizer();
        var cleanPayload = sanitizer.Sanitize(envelope.Payload);
        Assert.That(sanitizer.IsClean(cleanPayload), Is.True);

        // Resolve tenant
        var resolver = new TenantResolver();
        var tenant = resolver.Resolve(envelope.Metadata);
        Assert.That(tenant.IsResolved, Is.True);
        Assert.That(tenant.TenantId, Is.EqualTo("premium-corp"));

        // Verify envelope integrity
        Assert.That(envelope.Source, Is.EqualTo("OrderService"));
        Assert.That(envelope.MessageType, Is.EqualTo("order.created"));
    }

    // ── Challenge 2: Expiration and Priority ────────────────────────────────

    [Test]
    public void Challenge2_ExpirationAndPriority_CombinedScenario()
    {
        var urgentEnvelope = IntegrationEnvelope<string>.Create(
            "urgent-data", "AlertService", "alert.fired") with
        {
            Priority = MessagePriority.Critical,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
        };

        var expiredEnvelope = IntegrationEnvelope<string>.Create(
            "old-data", "BatchService", "batch.completed") with
        {
            Priority = MessagePriority.Low,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        };

        Assert.That(urgentEnvelope.Priority, Is.EqualTo(MessagePriority.Critical));
        Assert.That(urgentEnvelope.IsExpired, Is.False);

        Assert.That(expiredEnvelope.Priority, Is.EqualTo(MessagePriority.Low));
        Assert.That(expiredEnvelope.IsExpired, Is.True);

        // Best practice: check expiration before processing
        var toProcess = new[] { urgentEnvelope, expiredEnvelope }
            .Where(e => !e.IsExpired)
            .OrderByDescending(e => e.Priority)
            .ToList();

        Assert.That(toProcess, Has.Count.EqualTo(1));
        Assert.That(toProcess[0].MessageType, Is.EqualTo("alert.fired"));
    }

    // ── Challenge 3: Cross-Cutting Concerns Flow ────────────────────────────

    [Test]
    public void Challenge3_CrossCuttingFlow_SanitizeTenantValidate()
    {
        // Step 1: Create envelope with potentially unsafe data
        var envelope = IntegrationEnvelope<string>.Create(
            "SELECT * FROM users; --", "ExternalService", "data.imported") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = "acme-inc",
            },
            Priority = MessagePriority.High,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        // Step 2: Check not expired
        Assert.That(envelope.IsExpired, Is.False);

        // Step 3: Sanitize
        var sanitizer = new InputSanitizer();
        var clean = sanitizer.Sanitize(envelope.Payload);

        // Step 4: Resolve and verify tenant
        var resolver = new TenantResolver();
        var tenant = resolver.Resolve(envelope.Metadata);
        Assert.That(tenant.IsResolved, Is.True);
        Assert.That(tenant.TenantId, Is.EqualTo("acme-inc"));

        // Step 5: Verify isolation
        var guard = new TenantIsolationGuard(resolver);
        Assert.DoesNotThrow(() => guard.Enforce(envelope, "acme-inc"));

        // Step 6: Cross-tenant access should throw
        Assert.Throws<TenantIsolationException>(
            () => guard.Enforce(envelope, "other-tenant"));
    }
}
