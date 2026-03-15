using EnterpriseIntegrationPlatform.Processing.Routing;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class RouterOptionsTests
{
    [Fact]
    public void RouterOptions_DefaultRules_IsEmpty()
    {
        var options = new RouterOptions();

        options.Rules.Should().BeEmpty();
    }

    [Fact]
    public void RouterOptions_DefaultTopic_IsNull()
    {
        var options = new RouterOptions();

        options.DefaultTopic.Should().BeNull();
    }

    [Fact]
    public void RouterOptions_SectionName_IsExpectedValue()
    {
        RouterOptions.SectionName.Should().Be("ContentBasedRouter");
    }

    [Fact]
    public void RouterOptions_WithRulesAndDefaultTopic_ReturnsConfiguredValues()
    {
        var rule = new RoutingRule
        {
            Priority = 1,
            FieldName = "MessageType",
            Operator = RoutingOperator.Equals,
            Value = "OrderCreated",
            TargetTopic = "orders.created",
        };

        var options = new RouterOptions
        {
            Rules = [rule],
            DefaultTopic = "integration.default",
        };

        options.Rules.Should().ContainSingle().Which.Should().Be(rule);
        options.DefaultTopic.Should().Be("integration.default");
    }
}
