using EnterpriseIntegrationPlatform.Processing.Aggregator;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class AggregatorOptionsTests
{
    [Fact]
    public void AggregatorOptions_DefaultTargetTopic_IsEmpty()
    {
        var options = new AggregatorOptions();
        options.TargetTopic.Should().BeEmpty();
    }

    [Fact]
    public void AggregatorOptions_DefaultTargetMessageType_IsNull()
    {
        var options = new AggregatorOptions();
        options.TargetMessageType.Should().BeNull();
    }

    [Fact]
    public void AggregatorOptions_DefaultTargetSource_IsNull()
    {
        var options = new AggregatorOptions();
        options.TargetSource.Should().BeNull();
    }

    [Fact]
    public void AggregatorOptions_DefaultExpectedCount_IsZero()
    {
        var options = new AggregatorOptions();
        options.ExpectedCount.Should().Be(0);
    }

    [Fact]
    public void AggregatorOptions_CanSetTargetTopic()
    {
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        options.TargetTopic.Should().Be("orders.aggregated");
    }

    [Fact]
    public void AggregatorOptions_CanSetExpectedCount()
    {
        var options = new AggregatorOptions { ExpectedCount = 5 };
        options.ExpectedCount.Should().Be(5);
    }

    [Fact]
    public void AggregatorOptions_CanSetTargetMessageType()
    {
        var options = new AggregatorOptions { TargetMessageType = "OrderAggregated" };
        options.TargetMessageType.Should().Be("OrderAggregated");
    }

    [Fact]
    public void AggregatorOptions_CanSetTargetSource()
    {
        var options = new AggregatorOptions { TargetSource = "Aggregator" };
        options.TargetSource.Should().Be("Aggregator");
    }
}
