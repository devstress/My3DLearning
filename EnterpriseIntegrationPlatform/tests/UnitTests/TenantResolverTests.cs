using EnterpriseIntegrationPlatform.MultiTenancy;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class TenantResolverTests
{
    private TenantResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        _resolver = new();
    }

    [Test]
    public void Resolve_MetadataWithTenantId_ReturnsTenantContext()
    {
        var meta = new Dictionary<string, string> { [TenantResolver.TenantMetadataKey] = "tenant-abc" };
        var ctx = _resolver.Resolve(meta);
        Assert.That(ctx.TenantId, Is.EqualTo("tenant-abc"));
        Assert.That(ctx.IsResolved, Is.True);
    }

    [Test]
    public void Resolve_MetadataWithoutTenantId_ReturnsAnonymous()
    {
        var meta = new Dictionary<string, string> { ["other"] = "value" };
        var ctx = _resolver.Resolve(meta);
        Assert.That(ctx.IsResolved, Is.False);
        Assert.That(ctx.TenantId, Is.EqualTo("anonymous"));
    }

    [Test]
    public void Resolve_EmptyMetadata_ReturnsAnonymous()
    {
        var ctx = _resolver.Resolve(new Dictionary<string, string>());
        Assert.That(ctx.IsResolved, Is.False);
    }

    [Test]
    public void Resolve_NullMetadata_ThrowsArgumentNullException()
    {
        var act = () => _resolver.Resolve((IReadOnlyDictionary<string, string>)null!);
        Assert.Throws<ArgumentNullException>(() => act());
    }

    [Test]
    public void Resolve_String_WithTenantId_ReturnsTenantContext()
    {
        var ctx = _resolver.Resolve("tenant-xyz");
        Assert.That(ctx.TenantId, Is.EqualTo("tenant-xyz"));
        Assert.That(ctx.IsResolved, Is.True);
    }

    [Test]
    public void Resolve_NullString_ReturnsAnonymous()
    {
        var ctx = _resolver.Resolve((string?)null);
        Assert.That(ctx.IsResolved, Is.False);
    }

    [Test]
    public void Resolve_WhitespaceString_ReturnsAnonymous()
    {
        var ctx = _resolver.Resolve("   ");
        Assert.That(ctx.IsResolved, Is.False);
    }
}
