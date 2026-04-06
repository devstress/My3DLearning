// ============================================================================
// Tutorial 50 – Best Practices (Exam)
// ============================================================================
// E2E challenges: security + tenancy + expiration combined flow,
// priority-based processing, and cross-cutting concern integration
// via MockEndpoint.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.MultiTenancy;
using EnterpriseIntegrationPlatform.Security;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial50;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_SecurityTenancyFlow_EndToEnd()
    {
        await using var output = new MockEndpoint("e2e");
        var envelope = IntegrationEnvelope<string>.Create(
            "<script>alert('xss')</script> Order data", "OrderService", "order.created") with
        {
            Metadata = new Dictionary<string, string> { ["tenantId"] = "premium-corp" },
        };

        var sanitizer = new InputSanitizer();
        var clean = sanitizer.Sanitize(envelope.Payload);
        Assert.That(sanitizer.IsClean(clean), Is.True);

        var resolver = new TenantResolver();
        var tenant = resolver.Resolve(envelope.Metadata);
        Assert.That(tenant.IsResolved, Is.True);
        Assert.That(tenant.TenantId, Is.EqualTo("premium-corp"));

        var sanitized = IntegrationEnvelope<string>.Create(
            clean, envelope.Source, envelope.MessageType);
        await output.PublishAsync(sanitized, $"tenant.{tenant.TenantId}");
        output.AssertReceivedOnTopic("tenant.premium-corp", 1);
    }

    [Test]
    public async Task Challenge2_ExpirationPriority_ProcessesOnlyValid()
    {
        await using var output = new MockEndpoint("priority");
        var urgent = IntegrationEnvelope<string>.Create("urgent", "Alert", "alert.fired") with
        {
            Priority = MessagePriority.Critical,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
        };
        var expired = IntegrationEnvelope<string>.Create("old", "Batch", "batch.done") with
        {
            Priority = MessagePriority.Low,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        };

        var toProcess = new[] { urgent, expired }
            .Where(e => !e.IsExpired)
            .OrderByDescending(e => e.Priority)
            .ToList();

        foreach (var env in toProcess)
            await output.PublishAsync(env, "processed");

        Assert.That(toProcess, Has.Count.EqualTo(1));
        output.AssertReceivedOnTopic("processed", 1);
    }

    [Test]
    public async Task Challenge3_CrossCuttingFlow_SanitizeTenantPublish()
    {
        await using var output = new MockEndpoint("cross");
        var envelope = IntegrationEnvelope<string>.Create(
            "SELECT * FROM users; --", "External", "data.imported") with
        {
            Metadata = new Dictionary<string, string> { ["tenantId"] = "acme-inc" },
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        Assert.That(envelope.IsExpired, Is.False);

        var sanitizer = new InputSanitizer();
        var clean = sanitizer.Sanitize(envelope.Payload);

        var resolver = new TenantResolver();
        var tenant = resolver.Resolve(envelope.Metadata);
        Assert.That(tenant.IsResolved, Is.True);

        var guard = new TenantIsolationGuard(resolver);
        Assert.DoesNotThrow(() => guard.Enforce(envelope, "acme-inc"));
        Assert.Throws<TenantIsolationException>(() => guard.Enforce(envelope, "other-tenant"));

        var result = IntegrationEnvelope<string>.Create(clean, "pipeline", "data.processed");
        await output.PublishAsync(result, $"tenant.{tenant.TenantId}");
        output.AssertReceivedOnTopic("tenant.acme-inc", 1);
    }
}
