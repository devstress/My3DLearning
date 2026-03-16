using EnterpriseIntegrationPlatform.Processing.Splitter;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class SplitterOptionsTests
{
    [Fact]
    public void TargetTopic_Defaults_ToEmptyString()
    {
        var options = new SplitterOptions();

        options.TargetTopic.Should().BeEmpty();
    }

    [Fact]
    public void TargetTopic_CanBeSet()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };

        options.TargetTopic.Should().Be("items.split");
    }

    [Fact]
    public void TargetMessageType_Defaults_ToNull()
    {
        var options = new SplitterOptions();

        options.TargetMessageType.Should().BeNull();
    }

    [Fact]
    public void TargetMessageType_CanBeSet()
    {
        var options = new SplitterOptions { TargetMessageType = "ItemSplit" };

        options.TargetMessageType.Should().Be("ItemSplit");
    }

    [Fact]
    public void TargetSource_Defaults_ToNull()
    {
        var options = new SplitterOptions();

        options.TargetSource.Should().BeNull();
    }

    [Fact]
    public void TargetSource_CanBeSet()
    {
        var options = new SplitterOptions { TargetSource = "Splitter" };

        options.TargetSource.Should().Be("Splitter");
    }

    [Fact]
    public void ArrayPropertyName_Defaults_ToNull()
    {
        var options = new SplitterOptions();

        options.ArrayPropertyName.Should().BeNull();
    }

    [Fact]
    public void ArrayPropertyName_CanBeSet()
    {
        var options = new SplitterOptions { ArrayPropertyName = "items" };

        options.ArrayPropertyName.Should().Be("items");
    }
}
