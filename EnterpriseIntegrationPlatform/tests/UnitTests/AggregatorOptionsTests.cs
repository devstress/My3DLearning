using EnterpriseIntegrationPlatform.Processing.Aggregator;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class AggregatorOptionsTests
{
    [Test]
    public void AggregatorOptions_DefaultTargetTopic_IsEmpty()
    {
        var options = new AggregatorOptions();
        Assert.That(options.TargetTopic, Is.Empty);
    }

    [Test]
    public void AggregatorOptions_DefaultTargetMessageType_IsNull()
    {
        var options = new AggregatorOptions();
        Assert.That(options.TargetMessageType, Is.Null);
    }

    [Test]
    public void AggregatorOptions_DefaultTargetSource_IsNull()
    {
        var options = new AggregatorOptions();
        Assert.That(options.TargetSource, Is.Null);
    }

    [Test]
    public void AggregatorOptions_DefaultExpectedCount_IsZero()
    {
        var options = new AggregatorOptions();
        Assert.That(options.ExpectedCount, Is.EqualTo(0));
    }

    [Test]
    public void AggregatorOptions_CanSetTargetTopic()
    {
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        Assert.That(options.TargetTopic, Is.EqualTo("orders.aggregated"));
    }

    [Test]
    public void AggregatorOptions_CanSetExpectedCount()
    {
        var options = new AggregatorOptions { ExpectedCount = 5 };
        Assert.That(options.ExpectedCount, Is.EqualTo(5));
    }

    [Test]
    public void AggregatorOptions_CanSetTargetMessageType()
    {
        var options = new AggregatorOptions { TargetMessageType = "OrderAggregated" };
        Assert.That(options.TargetMessageType, Is.EqualTo("OrderAggregated"));
    }

    [Test]
    public void AggregatorOptions_CanSetTargetSource()
    {
        var options = new AggregatorOptions { TargetSource = "Aggregator" };
        Assert.That(options.TargetSource, Is.EqualTo("Aggregator"));
    }
}
