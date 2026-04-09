using EnterpriseIntegrationPlatform.Gateway.Api.Configuration;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.Gateway;

[TestFixture]
public class RouteDefinitionTests
{
    [Test]
    public void RequiresAuth_DefaultsToTrue()
    {
        var route = new RouteDefinition
        {
            Pattern = "/api/v1/admin",
            DownstreamService = "admin-api",
            DownstreamPath = "/api/admin",
        };
        Assert.That(route.RequiresAuth, Is.True);
    }

    [Test]
    public void RateLimitPolicy_DefaultsToNull()
    {
        var route = new RouteDefinition
        {
            Pattern = "/api/v1/admin",
            DownstreamService = "admin-api",
            DownstreamPath = "/api/admin",
        };
        Assert.That(route.RateLimitPolicy, Is.Null);
    }

    [Test]
    public void Properties_CanBeSet()
    {
        var route = new RouteDefinition
        {
            Pattern = "/api/v1/orders",
            DownstreamService = "order-service",
            DownstreamPath = "/api/orders",
            RequiresAuth = false,
            RateLimitPolicy = "strict",
        };

        Assert.That(route.Pattern, Is.EqualTo("/api/v1/orders"));
        Assert.That(route.DownstreamService, Is.EqualTo("order-service"));
        Assert.That(route.DownstreamPath, Is.EqualTo("/api/orders"));
        Assert.That(route.RequiresAuth, Is.False);
        Assert.That(route.RateLimitPolicy, Is.EqualTo("strict"));
    }
}
