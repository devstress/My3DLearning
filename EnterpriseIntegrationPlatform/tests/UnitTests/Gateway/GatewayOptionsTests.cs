using EnterpriseIntegrationPlatform.Gateway.Api.Configuration;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.Gateway;

[TestFixture]
public class GatewayOptionsTests
{
    [Test]
    public void SectionName_IsGateway()
    {
        Assert.That(GatewayOptions.SectionName, Is.EqualTo("Gateway"));
    }

    [Test]
    public void Defaults_AdminApiBaseUrl_IsLocalhost5200()
    {
        var options = new GatewayOptions();
        Assert.That(options.AdminApiBaseUrl, Is.EqualTo("http://localhost:5200"));
    }

    [Test]
    public void Defaults_OpenClawBaseUrl_IsLocalhost5100()
    {
        var options = new GatewayOptions();
        Assert.That(options.OpenClawBaseUrl, Is.EqualTo("http://localhost:5100"));
    }

    [Test]
    public void Defaults_RateLimitPerMinute_Is100()
    {
        var options = new GatewayOptions();
        Assert.That(options.RateLimitPerMinute, Is.EqualTo(100));
    }

    [Test]
    public void Defaults_GlobalRateLimitPerMinute_Is1000()
    {
        var options = new GatewayOptions();
        Assert.That(options.GlobalRateLimitPerMinute, Is.EqualTo(1000));
    }

    [Test]
    public void Defaults_RequireHttps_IsFalse()
    {
        var options = new GatewayOptions();
        Assert.That(options.RequireHttps, Is.False);
    }

    [Test]
    public void Defaults_AllowedOrigins_IsWildcard()
    {
        var options = new GatewayOptions();
        Assert.That(options.AllowedOrigins, Is.EqualTo(new[] { "*" }));
    }
}
