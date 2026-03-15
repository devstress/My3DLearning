using EnterpriseIntegrationPlatform.Demo.Pipeline;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class PipelineOptionsTests
{
    [Fact]
    public void NatsUrl_DefaultsToLocalhost()
    {
        var options = new PipelineOptions();

        options.NatsUrl.Should().Be("nats://localhost:15222");
    }

    [Fact]
    public void InboundSubject_DefaultsToIntegrationInbound()
    {
        var options = new PipelineOptions();

        options.InboundSubject.Should().Be("integration.inbound");
    }

    [Fact]
    public void AckSubject_DefaultsToIntegrationAck()
    {
        var options = new PipelineOptions();

        options.AckSubject.Should().Be("integration.ack");
    }

    [Fact]
    public void NackSubject_DefaultsToIntegrationNack()
    {
        var options = new PipelineOptions();

        options.NackSubject.Should().Be("integration.nack");
    }

    [Fact]
    public void ConsumerGroup_DefaultsToDemoPipeline()
    {
        var options = new PipelineOptions();

        options.ConsumerGroup.Should().Be("demo-pipeline");
    }

    [Fact]
    public void TemporalServerAddress_DefaultsToLocalhost()
    {
        var options = new PipelineOptions();

        options.TemporalServerAddress.Should().Be("localhost:15233");
    }

    [Fact]
    public void TemporalNamespace_DefaultsToDefault()
    {
        var options = new PipelineOptions();

        options.TemporalNamespace.Should().Be("default");
    }

    [Fact]
    public void TemporalTaskQueue_DefaultsToIntegrationWorkflows()
    {
        var options = new PipelineOptions();

        options.TemporalTaskQueue.Should().Be("integration-workflows");
    }

    [Fact]
    public void WorkflowTimeout_DefaultsToFiveMinutes()
    {
        var options = new PipelineOptions();

        options.WorkflowTimeout.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void SectionName_IsConstant()
    {
        PipelineOptions.SectionName.Should().Be("Pipeline");
    }

    [Fact]
    public void AllProperties_AcceptCustomValues()
    {
        var options = new PipelineOptions
        {
            NatsUrl = "nats://custom:4222",
            InboundSubject = "custom.inbound",
            AckSubject = "custom.ack",
            NackSubject = "custom.nack",
            ConsumerGroup = "custom-group",
            TemporalServerAddress = "temporal:7233",
            TemporalNamespace = "production",
            TemporalTaskQueue = "production-workflows",
            WorkflowTimeout = TimeSpan.FromMinutes(10),
        };

        options.NatsUrl.Should().Be("nats://custom:4222");
        options.InboundSubject.Should().Be("custom.inbound");
        options.AckSubject.Should().Be("custom.ack");
        options.NackSubject.Should().Be("custom.nack");
        options.ConsumerGroup.Should().Be("custom-group");
        options.TemporalServerAddress.Should().Be("temporal:7233");
        options.TemporalNamespace.Should().Be("production");
        options.TemporalTaskQueue.Should().Be("production-workflows");
        options.WorkflowTimeout.Should().Be(TimeSpan.FromMinutes(10));
    }
}
