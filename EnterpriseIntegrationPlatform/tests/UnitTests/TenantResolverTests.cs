using EnterpriseIntegrationPlatform.MultiTenancy;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class TenantResolverTests
{
    private readonly TenantResolver _resolver = new();

    [Fact]
    public void Resolve_MetadataWithTenantId_ReturnsTenantContext()
    {
        var meta = new Dictionary<string, string> { [TenantResolver.TenantMetadataKey] = "tenant-abc" };
        var ctx = _resolver.Resolve(meta);
        ctx.TenantId.Should().Be("tenant-abc");
        ctx.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void Resolve_MetadataWithoutTenantId_ReturnsAnonymous()
    {
        var meta = new Dictionary<string, string> { ["other"] = "value" };
        var ctx = _resolver.Resolve(meta);
        ctx.IsResolved.Should().BeFalse();
        ctx.TenantId.Should().Be("anonymous");
    }

    [Fact]
    public void Resolve_EmptyMetadata_ReturnsAnonymous()
    {
        var ctx = _resolver.Resolve(new Dictionary<string, string>());
        ctx.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void Resolve_NullMetadata_ThrowsArgumentNullException()
    {
        var act = () => _resolver.Resolve((IReadOnlyDictionary<string, string>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Resolve_String_WithTenantId_ReturnsTenantContext()
    {
        var ctx = _resolver.Resolve("tenant-xyz");
        ctx.TenantId.Should().Be("tenant-xyz");
        ctx.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void Resolve_NullString_ReturnsAnonymous()
    {
        var ctx = _resolver.Resolve((string?)null);
        ctx.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WhitespaceString_ReturnsAnonymous()
    {
        var ctx = _resolver.Resolve("   ");
        ctx.IsResolved.Should().BeFalse();
    }
}
