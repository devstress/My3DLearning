using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.MultiTenancy;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class TenantIsolationGuardTests
{
    private readonly TenantIsolationGuard _guard = new(new TenantResolver());

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

    [Fact]
    public void Enforce_MatchingTenant_DoesNotThrow()
    {
        var envelope = BuildEnvelope("tenant-a");
        var act = () => _guard.Enforce(envelope, "tenant-a");
        act.Should().NotThrow();
    }

    [Fact]
    public void Enforce_WrongTenant_ThrowsTenantIsolationException()
    {
        var envelope = BuildEnvelope("tenant-a");
        var act = () => _guard.Enforce(envelope, "tenant-b");
        act.Should().Throw<TenantIsolationException>()
            .Which.ActualTenantId.Should().Be("tenant-a");
    }

    [Fact]
    public void Enforce_MissingTenant_ThrowsTenantIsolationException()
    {
        var envelope = BuildEnvelope(tenantId: null);
        var act = () => _guard.Enforce(envelope, "tenant-a");
        act.Should().Throw<TenantIsolationException>()
            .Which.ActualTenantId.Should().BeNull();
    }

    [Fact]
    public void Enforce_NullEnvelope_ThrowsArgumentNullException()
    {
        var act = () => _guard.Enforce((IntegrationEnvelope<string>)null!, "tenant-a");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enforce_EmptyExpectedTenantId_ThrowsArgumentException()
    {
        var envelope = BuildEnvelope("tenant-a");
        var act = () => _guard.Enforce(envelope, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TenantIsolationException_ExposesExpectedTenantId()
    {
        var envelope = BuildEnvelope("actual-tenant");
        var act = () => _guard.Enforce(envelope, "expected-tenant");
        act.Should().Throw<TenantIsolationException>()
            .Which.ExpectedTenantId.Should().Be("expected-tenant");
    }

    [Fact]
    public void TenantIsolationException_ExposesMessageId()
    {
        var envelope = BuildEnvelope("a");
        var act = () => _guard.Enforce(envelope, "b");
        act.Should().Throw<TenantIsolationException>()
            .Which.MessageId.Should().Be(envelope.MessageId);
    }
}
