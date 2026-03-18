using EnterpriseIntegrationPlatform.Processing.Replay;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class ReplayOptionsTests
{
    [Fact]
    public void MaxMessages_Default_Is1000()
    {
        var options = new ReplayOptions();
        options.MaxMessages.Should().Be(1000);
    }

    [Fact]
    public void BatchSize_Default_Is100()
    {
        var options = new ReplayOptions();
        options.BatchSize.Should().Be(100);
    }

    [Fact]
    public void SourceTopic_Default_IsEmptyString()
    {
        var options = new ReplayOptions();
        options.SourceTopic.Should().BeEmpty();
    }

    [Fact]
    public void TargetTopic_Default_IsEmptyString()
    {
        var options = new ReplayOptions();
        options.TargetTopic.Should().BeEmpty();
    }

    [Fact]
    public void Properties_SetValues_ReturnCorrectValues()
    {
        var options = new ReplayOptions
        {
            SourceTopic = "events.source",
            TargetTopic = "events.target",
            MaxMessages = 500,
            BatchSize = 50
        };

        options.SourceTopic.Should().Be("events.source");
        options.TargetTopic.Should().Be("events.target");
        options.MaxMessages.Should().Be(500);
        options.BatchSize.Should().Be(50);
    }
}
