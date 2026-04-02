using EnterpriseIntegrationPlatform.Processing.Routing;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class RouterOptionsTests
{
    [Test]
    public void RouterOptions_DefaultRules_IsEmpty()
    {
        var options = new RouterOptions();

        Assert.That(options.Rules, Is.Empty);
    }

    [Test]
    public void RouterOptions_DefaultTopic_IsNull()
    {
        var options = new RouterOptions();

        Assert.That(options.DefaultTopic, Is.Null);
    }

    [Test]
    public void RouterOptions_SectionName_IsExpectedValue()
    {
        Assert.That(RouterOptions.SectionName, Is.EqualTo("ContentBasedRouter"));
    }

    [Test]
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

        Assert.That(options.Rules, Has.Count.EqualTo(1));
        Assert.That(options.Rules[0], Is.EqualTo(rule));
        Assert.That(options.DefaultTopic, Is.EqualTo("integration.default"));
    }
}
