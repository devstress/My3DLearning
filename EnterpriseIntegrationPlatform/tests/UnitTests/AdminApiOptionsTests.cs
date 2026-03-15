using EnterpriseIntegrationPlatform.Admin.Api;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class AdminApiOptionsTests
{
    [Fact]
    public void ApiKeys_DefaultsToEmptyList()
    {
        var options = new AdminApiOptions();

        options.ApiKeys.Should().BeEmpty();
    }

    [Fact]
    public void RateLimitPerMinute_DefaultsToSixty()
    {
        var options = new AdminApiOptions();

        options.RateLimitPerMinute.Should().Be(60);
    }

    [Fact]
    public void ApiKeys_AcceptsMultipleKeys()
    {
        var keys = new[] { "key-a", "key-b", "key-c" };

        var options = new AdminApiOptions { ApiKeys = keys };

        options.ApiKeys.Should().BeEquivalentTo(keys);
    }

    [Fact]
    public void RateLimitPerMinute_AcceptsCustomValue()
    {
        var options = new AdminApiOptions { RateLimitPerMinute = 120 };

        options.RateLimitPerMinute.Should().Be(120);
    }

    [Fact]
    public void ApiKeys_IsCaseSensitiveByDefault()
    {
        var options = new AdminApiOptions { ApiKeys = ["MyKey"] };

        options.ApiKeys.Should().Contain("MyKey");
        options.ApiKeys.Should().NotContain("mykey");
    }
}
