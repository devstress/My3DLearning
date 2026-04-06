// ============================================================================
// Tutorial 32 – Multi-Tenancy (Lab)
// ============================================================================
// This lab exercises TenantResolver, TenantIsolationGuard, TenantContext,
// and TenantIsolationException to learn multi-tenant message handling.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.MultiTenancy;
using NUnit.Framework;

namespace TutorialLabs.Tutorial32;

[TestFixture]
public sealed class Lab
{
    private TenantResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        _resolver = new TenantResolver();
    }

    // ── Resolve From Metadata With tenantId Key ─────────────────────────────

    [Test]
    public void Resolve_FromMetadata_WithTenantIdKey()
    {
        var metadata = new Dictionary<string, string>
        {
            [TenantResolver.TenantMetadataKey] = "tenant-abc",
        };

        var context = _resolver.Resolve(metadata);

        Assert.That(context.TenantId, Is.EqualTo("tenant-abc"));
        Assert.That(context.IsResolved, Is.True);
    }

    // ── Resolve Returns Anonymous For Missing tenantId ──────────────────────

    [Test]
    public void Resolve_MissingTenantId_ReturnsAnonymous()
    {
        var metadata = new Dictionary<string, string>();

        var context = _resolver.Resolve(metadata);

        Assert.That(context.IsResolved, Is.False);
        Assert.That(context, Is.SameAs(TenantContext.Anonymous));
    }

    // ── Resolve(string) With Explicit TenantId ──────────────────────────────

    [Test]
    public void Resolve_String_WithExplicitTenantId()
    {
        var context = _resolver.Resolve("my-tenant");

        Assert.That(context.TenantId, Is.EqualTo("my-tenant"));
        Assert.That(context.IsResolved, Is.True);
    }

    // ── TenantIsolationGuard Passes When Tenant Matches ─────────────────────

    [Test]
    public void IsolationGuard_Enforce_PassesWhenTenantMatches()
    {
        var guard = new TenantIsolationGuard(_resolver);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "event") with
        {
            Metadata = new Dictionary<string, string>
            {
                [TenantResolver.TenantMetadataKey] = "tenant-x",
            },
        };

        Assert.DoesNotThrow(() => guard.Enforce(envelope, "tenant-x"));
    }

    // ── TenantIsolationGuard Throws On Mismatch ─────────────────────────────

    [Test]
    public void IsolationGuard_Enforce_ThrowsOnMismatch()
    {
        var guard = new TenantIsolationGuard(_resolver);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "event") with
        {
            Metadata = new Dictionary<string, string>
            {
                [TenantResolver.TenantMetadataKey] = "tenant-a",
            },
        };

        var ex = Assert.Throws<TenantIsolationException>(
            () => guard.Enforce(envelope, "tenant-b"));

        Assert.That(ex!.ActualTenantId, Is.EqualTo("tenant-a"));
        Assert.That(ex.ExpectedTenantId, Is.EqualTo("tenant-b"));
    }

    // ── TenantContext.Anonymous Has Expected Defaults ────────────────────────

    [Test]
    public void TenantContext_Anonymous_HasExpectedDefaults()
    {
        var anon = TenantContext.Anonymous;

        Assert.That(anon.TenantId, Is.EqualTo("anonymous"));
        Assert.That(anon.IsResolved, Is.False);
        Assert.That(anon.TenantName, Is.Null);
    }

    // ── TenantIsolationException Captures Fields ────────────────────────────

    [Test]
    public void TenantIsolationException_CapturesFields()
    {
        var msgId = Guid.NewGuid();
        var ex = new TenantIsolationException(msgId, "actual-t", "expected-t", "details");

        Assert.That(ex.MessageId, Is.EqualTo(msgId));
        Assert.That(ex.ActualTenantId, Is.EqualTo("actual-t"));
        Assert.That(ex.ExpectedTenantId, Is.EqualTo("expected-t"));
        Assert.That(ex.Message, Is.EqualTo("details"));
    }
}
