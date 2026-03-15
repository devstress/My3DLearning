using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Workflow.Temporal;

namespace EnterpriseIntegrationPlatform.Tests.Workflow;

public class TemporalOptionsTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new TemporalOptions();

        options.ServerAddress.Should().Be("localhost:7233");
        options.Namespace.Should().Be("default");
        options.TaskQueue.Should().Be("integration-workflows");
    }

    [Fact]
    public void SectionName_ShouldBeTemporal()
    {
        TemporalOptions.SectionName.Should().Be("Temporal");
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var options = new TemporalOptions
        {
            ServerAddress = "temporal.prod:7233",
            Namespace = "production",
            TaskQueue = "custom-queue",
        };

        options.ServerAddress.Should().Be("temporal.prod:7233");
        options.Namespace.Should().Be("production");
        options.TaskQueue.Should().Be("custom-queue");
    }
}
