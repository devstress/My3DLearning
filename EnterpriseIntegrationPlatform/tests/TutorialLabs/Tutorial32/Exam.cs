// ============================================================================
// Tutorial 32 – Multi-Tenancy (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — multi tenant routing_ isolates per tenant
//   🟡 Intermediate  — cross tenant access_ rejected
//   🔴 Advanced      — anonymous tenant_ guard rejects
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.MultiTenancy;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial32;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_MultiTenantRouting_IsolatesPerTenant()
    {
        await using var output = new MockEndpoint("exam-tenant");
        // TODO: Create a TenantResolver with appropriate configuration
        dynamic resolver = null!;

        var tenants = new[] { "alpha", "beta", "gamma" };
        foreach (var tid in tenants)
        {
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: Set metadata - envelope.Metadata["tenantId"] = tid;
            // TODO: var ctx = resolver.Resolve(...)
            dynamic ctx = null!;
            Assert.That(ctx.IsResolved, Is.True);
            // TODO: await output.PublishAsync(...)
        }

        output.AssertReceivedCount(3);
        output.AssertReceivedOnTopic("orders.alpha", 1);
        output.AssertReceivedOnTopic("orders.beta", 1);
        output.AssertReceivedOnTopic("orders.gamma", 1);
    }

    [Test]
    public async Task Intermediate_CrossTenantAccess_Rejected()
    {
        await using var output = new MockEndpoint("exam-reject");
        // TODO: Create a TenantResolver with appropriate configuration
        dynamic resolver = null!;
        // TODO: Create a TenantIsolationGuard with appropriate configuration
        dynamic guard = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: Set metadata - envelope.Metadata["tenantId"] = "tenant-a";

        var ex = Assert.Throws<TenantIsolationException>(
            () => guard.Enforce(envelope, "tenant-b"));

        Assert.That(ex!.ActualTenantId, Is.EqualTo("tenant-a"));
        Assert.That(ex.ExpectedTenantId, Is.EqualTo("tenant-b"));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic alert = null!;
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("violations", 1);
    }

    [Test]
    public async Task Advanced_AnonymousTenant_GuardRejects()
    {
        await using var output = new MockEndpoint("exam-anon");
        // TODO: Create a TenantResolver with appropriate configuration
        dynamic resolver = null!;
        // TODO: Create a TenantIsolationGuard with appropriate configuration
        dynamic guard = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;

        var ex = Assert.Throws<TenantIsolationException>(
            () => guard.Enforce(envelope, "expected-tenant"));

        Assert.That(ex!.ActualTenantId, Is.Null);
        Assert.That(ex.ExpectedTenantId, Is.EqualTo("expected-tenant"));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic notification = null!;
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("security", 1);
    }
}
#endif
