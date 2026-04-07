// ============================================================================
// Tutorial 32 – Multi-Tenancy (Lab)
// ============================================================================
// EIP Pattern: Multi-Tenant Messaging
// E2E: TenantResolver + TenantIsolationGuard resolve and enforce tenant
//      boundaries, with MockEndpoint for tenant-scoped publishing.
// ============================================================================
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.MultiTenancy;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial32;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("tenant-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();


    // ── 1. Tenant Resolution ─────────────────────────────────────────

    [Test]
    public async Task ResolveFromMetadata_ReturnsTenantContext()
    {
        var resolver = new TenantResolver();
        var metadata = new Dictionary<string, string>
        {
            { TenantResolver.TenantMetadataKey, "acme" },
        };
        var ctx = resolver.Resolve(metadata);

        Assert.That(ctx.IsResolved, Is.True);
        Assert.That(ctx.TenantId, Is.EqualTo("acme"));

        var envelope = IntegrationEnvelope<string>.Create("ok", "resolver", "TenantResolved");
        await _output.PublishAsync(envelope, $"tenant.{ctx.TenantId}", default);
        _output.AssertReceivedOnTopic("tenant.acme", 1);
    }

    [Test]
    public async Task ResolveFromMetadata_MissingKey_ReturnsAnonymous()
    {
        var resolver = new TenantResolver();
        var ctx = resolver.Resolve(new Dictionary<string, string>());

        Assert.That(ctx.IsResolved, Is.False);
        Assert.That(ctx.TenantId, Is.EqualTo("anonymous"));
        await Task.CompletedTask;
    }

    [Test]
    public async Task ResolveFromString_ReturnsTenantContext()
    {
        var resolver = new TenantResolver();
        var ctx = resolver.Resolve("tenant-42");

        Assert.That(ctx.IsResolved, Is.True);
        Assert.That(ctx.TenantId, Is.EqualTo("tenant-42"));
        await Task.CompletedTask;
    }


    // ── 2. Tenant Isolation ──────────────────────────────────────────

    [Test]
    public async Task ResolveFromString_NullOrWhitespace_ReturnsAnonymous()
    {
        var resolver = new TenantResolver();

        Assert.That(resolver.Resolve((string?)null).IsResolved, Is.False);
        Assert.That(resolver.Resolve("   ").IsResolved, Is.False);
        await Task.CompletedTask;
    }

    [Test]
    public async Task IsolationGuard_MatchingTenant_DoesNotThrow()
    {
        var resolver = new TenantResolver();
        var guard = new TenantIsolationGuard(resolver);
        var envelope = IntegrationEnvelope<string>.Create("data", "src", "type");
        envelope.Metadata["tenantId"] = "acme";

        Assert.DoesNotThrow(() => guard.Enforce(envelope, "acme"));
        await Task.CompletedTask;
    }

    [Test]
    public async Task IsolationGuard_MismatchedTenant_ThrowsAndPublishesAlert()
    {
        var resolver = new TenantResolver();
        var guard = new TenantIsolationGuard(resolver);
        var envelope = IntegrationEnvelope<string>.Create("data", "src", "type");
        envelope.Metadata["tenantId"] = "acme";

        var ex = Assert.Throws<TenantIsolationException>(
            () => guard.Enforce(envelope, "globex"));

        Assert.That(ex!.ExpectedTenantId, Is.EqualTo("globex"));
        Assert.That(ex.ActualTenantId, Is.EqualTo("acme"));

        var alert = IntegrationEnvelope<string>.Create(
            $"Cross-tenant violation: {ex.ActualTenantId} vs {ex.ExpectedTenantId}",
            "guard", "TenantViolation");
        await _output.PublishAsync(alert, "security-alerts", default);
        _output.AssertReceivedOnTopic("security-alerts", 1);
    }
}
