using EnterpriseIntegrationPlatform.Admin.Api;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class AdminApiOptionsTests
{
    [Test]
    public void ApiKeys_DefaultsToEmptyList()
    {
        var options = new AdminApiOptions();

        Assert.That(options.ApiKeys, Is.Empty);
    }

    [Test]
    public void RateLimitPerMinute_DefaultsToSixty()
    {
        var options = new AdminApiOptions();

        Assert.That(options.RateLimitPerMinute, Is.EqualTo(60));
    }

    [Test]
    public void ApiKeys_AcceptsMultipleKeys()
    {
        var keys = new[] { "key-a", "key-b", "key-c" };

        var options = new AdminApiOptions { ApiKeys = keys };

        Assert.That(options.ApiKeys, Is.EquivalentTo(keys));
    }

    [Test]
    public void RateLimitPerMinute_AcceptsCustomValue()
    {
        var options = new AdminApiOptions { RateLimitPerMinute = 120 };

        Assert.That(options.RateLimitPerMinute, Is.EqualTo(120));
    }

    [Test]
    public void ApiKeys_IsCaseSensitiveByDefault()
    {
        var options = new AdminApiOptions { ApiKeys = ["MyKey"] };

        Assert.That(options.ApiKeys, Does.Contain("MyKey"));
        Assert.That(options.ApiKeys, Does.Not.Contain("mykey"));
    }
}
