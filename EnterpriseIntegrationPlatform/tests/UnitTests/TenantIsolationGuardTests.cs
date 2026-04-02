using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.MultiTenancy;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class TenantIsolationGuardTests
{
    private TenantIsolationGuard _guard = null!;

    [SetUp]
    public void SetUp()
    {
        _guard = new(new TenantResolver());
    }

    private static IntegrationEnvelope<string> BuildEnvelope(string? tenantId = null)
    {
        var meta = new Dictionary<string, string>();
        if (tenantId is not null)
            meta[TenantResolver.TenantMetadataKey] = tenantId;

        return new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "test",
            MessageType = "Test",
            Payload = "payload",
            Metadata = meta,
        };
    }

    [Test]
    public void Enforce_MatchingTenant_DoesNotThrow()
    {
        var envelope = BuildEnvelope("tenant-a");
        var act = () => _guard.Enforce(envelope, "tenant-a");
        Assert.DoesNotThrow(() => act());
    }

    [Test]
    public void Enforce_WrongTenant_ThrowsTenantIsolationException()
    {
        var envelope = BuildEnvelope("tenant-a");
        var act = () => _guard.Enforce(envelope, "tenant-b");
        var ex = Assert.Throws<TenantIsolationException>(() => act());
        Assert.That(ex!.ActualTenantId, Is.EqualTo("tenant-a"));
    }

    [Test]
    public void Enforce_MissingTenant_ThrowsTenantIsolationException()
    {
        var envelope = BuildEnvelope(tenantId: null);
        var act = () => _guard.Enforce(envelope, "tenant-a");
        var ex = Assert.Throws<TenantIsolationException>(() => act());
        Assert.That(ex!.ActualTenantId, Is.Null);
    }

    [Test]
    public void Enforce_NullEnvelope_ThrowsArgumentNullException()
    {
        var act = () => _guard.Enforce((IntegrationEnvelope<string>)null!, "tenant-a");
        Assert.Throws<ArgumentNullException>(() => act());
    }

    [Test]
    public void Enforce_EmptyExpectedTenantId_ThrowsArgumentException()
    {
        var envelope = BuildEnvelope("tenant-a");
        var act = () => _guard.Enforce(envelope, "");
        Assert.Throws<ArgumentException>(() => act());
    }

    [Test]
    public void TenantIsolationException_ExposesExpectedTenantId()
    {
        var envelope = BuildEnvelope("actual-tenant");
        var act = () => _guard.Enforce(envelope, "expected-tenant");
        var ex = Assert.Throws<TenantIsolationException>(() => act());
        Assert.That(ex!.ExpectedTenantId, Is.EqualTo("expected-tenant"));
    }

    [Test]
    public void TenantIsolationException_ExposesMessageId()
    {
        var envelope = BuildEnvelope("a");
        var act = () => _guard.Enforce(envelope, "b");
        var ex = Assert.Throws<TenantIsolationException>(() => act());
        Assert.That(ex!.MessageId, Is.EqualTo(envelope.MessageId));
    }
}
