using EnterpriseIntegrationPlatform.Gateway.Api.Configuration;
using EnterpriseIntegrationPlatform.Gateway.Api.Routing;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.Gateway;

[TestFixture]
public sealed class RouteResolverTests
{
    private RouteResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        var options = Options.Create(new GatewayOptions
        {
            AdminApiBaseUrl = "http://admin-api:5200",
            OpenClawBaseUrl = "http://openclaw:5100",
        });
        _resolver = new RouteResolver(options);
    }

    [Test]
    public void Resolve_AdminRoute_ResolvesToAdminApi()
    {
        var result = _resolver.Resolve("/api/v1/admin/status");

        Assert.That(result, Is.EqualTo("http://admin-api:5200/api/admin/status"));
    }

    [Test]
    public void Resolve_InspectRoute_ResolvesToOpenClaw()
    {
        var result = _resolver.Resolve("/api/v1/inspect/messages");

        Assert.That(result, Is.EqualTo("http://openclaw:5100/api/inspect/messages"));
    }

    [Test]
    public void Resolve_UnknownRoute_ReturnsNull()
    {
        var result = _resolver.Resolve("/api/v1/unknown/path");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Resolve_VersionedRoute_V2_ResolvesCorrectly()
    {
        var result = _resolver.Resolve("/api/v2/admin/messages/123");

        Assert.That(result, Is.EqualTo("http://admin-api:5200/api/admin/messages/123"));
    }

    [Test]
    public void Resolve_AdminRootPath_ResolvesCorrectly()
    {
        var result = _resolver.Resolve("/api/v1/admin");

        Assert.That(result, Is.EqualTo("http://admin-api:5200/api/admin"));
    }
}
