// ============================================================================
// Tutorial 32 – Multi-Tenancy (Exam)
// ============================================================================
// Coding challenges: multi-tenant routing from metadata, cross-tenant
// rejection scenario, and anonymous tenant handling in the guard.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.MultiTenancy;
using NUnit.Framework;

namespace TutorialLabs.Tutorial32;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Resolve Tenant From Metadata, Verify Isolation ──────────

    [Test]
    public void Challenge1_MultiTenantRouting_ResolveAndVerifyIsolation()
    {
        var resolver = new TenantResolver();
        var guard = new TenantIsolationGuard(resolver);

        // Simulate two tenants sending messages
        var envTenantA = IntegrationEnvelope<string>.Create("orderA", "OrderService", "order.created") with
        {
            Metadata = new Dictionary<string, string>
            {
                [TenantResolver.TenantMetadataKey] = "acme-corp",
            },
        };

        var envTenantB = IntegrationEnvelope<string>.Create("orderB", "OrderService", "order.created") with
        {
            Metadata = new Dictionary<string, string>
            {
                [TenantResolver.TenantMetadataKey] = "globex-inc",
            },
        };

        // Resolve each tenant
        var ctxA = resolver.Resolve(envTenantA.Metadata);
        var ctxB = resolver.Resolve(envTenantB.Metadata);
        Assert.That(ctxA.TenantId, Is.EqualTo("acme-corp"));
        Assert.That(ctxB.TenantId, Is.EqualTo("globex-inc"));

        // Guard passes for correct tenant
        Assert.DoesNotThrow(() => guard.Enforce(envTenantA, "acme-corp"));
        Assert.DoesNotThrow(() => guard.Enforce(envTenantB, "globex-inc"));
    }

    // ── Challenge 2: Cross-Tenant Rejection ─────────────────────────────────

    [Test]
    public void Challenge2_CrossTenantRejection()
    {
        var resolver = new TenantResolver();
        var guard = new TenantIsolationGuard(resolver);

        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "event") with
        {
            Metadata = new Dictionary<string, string>
            {
                [TenantResolver.TenantMetadataKey] = "tenant-alpha",
            },
        };

        // Attempt to process in wrong tenant context
        var ex = Assert.Throws<TenantIsolationException>(
            () => guard.Enforce(envelope, "tenant-beta"));

        Assert.That(ex!.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(ex.ActualTenantId, Is.EqualTo("tenant-alpha"));
        Assert.That(ex.ExpectedTenantId, Is.EqualTo("tenant-beta"));
        Assert.That(ex.Message, Does.Contain("tenant-alpha"));
        Assert.That(ex.Message, Does.Contain("tenant-beta"));
    }

    // ── Challenge 3: Anonymous Tenant Handling in Guard ──────────────────────

    [Test]
    public void Challenge3_AnonymousTenant_GuardThrows()
    {
        var resolver = new TenantResolver();
        var guard = new TenantIsolationGuard(resolver);

        // Envelope with no tenantId metadata → resolves to Anonymous
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "event");

        var ex = Assert.Throws<TenantIsolationException>(
            () => guard.Enforce(envelope, "required-tenant"));

        // Anonymous is not resolved, so ActualTenantId should be null
        Assert.That(ex!.ActualTenantId, Is.Null);
        Assert.That(ex.ExpectedTenantId, Is.EqualTo("required-tenant"));
        Assert.That(ex.Message, Does.Contain("tenant identifier"));
    }
}
