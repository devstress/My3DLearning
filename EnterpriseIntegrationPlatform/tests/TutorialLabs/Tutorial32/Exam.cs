// ============================================================================
// Tutorial 32 – Multi-Tenancy (Exam)
// ============================================================================
// EIP Pattern: Multi-Tenant Messaging
// E2E: Multi-tenant routing, cross-tenant rejection, and anonymous tenant
//      guard behavior with MockEndpoint verification.
// ============================================================================
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.MultiTenancy;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial32;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_MultiTenantRouting_IsolatesPerTenant()
    {
        await using var output = new MockEndpoint("exam-tenant");
        var resolver = new TenantResolver();

        var tenants = new[] { "alpha", "beta", "gamma" };
        foreach (var tid in tenants)
        {
            var envelope = IntegrationEnvelope<string>.Create($"data-{tid}", "src", "Order");
            envelope.Metadata["tenantId"] = tid;
            var ctx = resolver.Resolve(envelope.Metadata);
            Assert.That(ctx.IsResolved, Is.True);
            await output.PublishAsync(envelope, $"orders.{ctx.TenantId}", default);
        }

        output.AssertReceivedCount(3);
        output.AssertReceivedOnTopic("orders.alpha", 1);
        output.AssertReceivedOnTopic("orders.beta", 1);
        output.AssertReceivedOnTopic("orders.gamma", 1);
    }

    [Test]
    public async Task Challenge2_CrossTenantAccess_Rejected()
    {
        await using var output = new MockEndpoint("exam-reject");
        var resolver = new TenantResolver();
        var guard = new TenantIsolationGuard(resolver);

        var envelope = IntegrationEnvelope<string>.Create("secret", "src", "Data");
        envelope.Metadata["tenantId"] = "tenant-a";

        var ex = Assert.Throws<TenantIsolationException>(
            () => guard.Enforce(envelope, "tenant-b"));

        Assert.That(ex!.ActualTenantId, Is.EqualTo("tenant-a"));
        Assert.That(ex.ExpectedTenantId, Is.EqualTo("tenant-b"));

        var alert = IntegrationEnvelope<string>.Create(ex.Message, "guard", "Violation");
        await output.PublishAsync(alert, "violations", default);
        output.AssertReceivedOnTopic("violations", 1);
    }

    [Test]
    public async Task Challenge3_AnonymousTenant_GuardRejects()
    {
        await using var output = new MockEndpoint("exam-anon");
        var resolver = new TenantResolver();
        var guard = new TenantIsolationGuard(resolver);

        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "Msg");

        var ex = Assert.Throws<TenantIsolationException>(
            () => guard.Enforce(envelope, "expected-tenant"));

        Assert.That(ex!.ActualTenantId, Is.Null);
        Assert.That(ex.ExpectedTenantId, Is.EqualTo("expected-tenant"));

        var notification = IntegrationEnvelope<string>.Create(
            "Anonymous access attempt", "guard", "AnonymousRejected");
        await output.PublishAsync(notification, "security", default);
        output.AssertReceivedOnTopic("security", 1);
    }
}
