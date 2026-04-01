using EnterpriseIntegrationPlatform.Demo.Pipeline;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PipelineOptionsTests
{
    [Test]
    public void NatsUrl_DefaultsToLocalhost()
    {
        var options = new PipelineOptions();

        Assert.That(options.NatsUrl, Is.EqualTo("nats://localhost:15222"));
    }

    [Test]
    public void InboundSubject_DefaultsToIntegrationInbound()
    {
        var options = new PipelineOptions();

        Assert.That(options.InboundSubject, Is.EqualTo("integration.inbound"));
    }

    [Test]
    public void AckSubject_DefaultsToIntegrationAck()
    {
        var options = new PipelineOptions();

        Assert.That(options.AckSubject, Is.EqualTo("integration.ack"));
    }

    [Test]
    public void NackSubject_DefaultsToIntegrationNack()
    {
        var options = new PipelineOptions();

        Assert.That(options.NackSubject, Is.EqualTo("integration.nack"));
    }

    [Test]
    public void ConsumerGroup_DefaultsToDemoPipeline()
    {
        var options = new PipelineOptions();

        Assert.That(options.ConsumerGroup, Is.EqualTo("demo-pipeline"));
    }

    [Test]
    public void TemporalServerAddress_DefaultsToLocalhost()
    {
        var options = new PipelineOptions();

        Assert.That(options.TemporalServerAddress, Is.EqualTo("localhost:15233"));
    }

    [Test]
    public void TemporalNamespace_DefaultsToDefault()
    {
        var options = new PipelineOptions();

        Assert.That(options.TemporalNamespace, Is.EqualTo("default"));
    }

    [Test]
    public void TemporalTaskQueue_DefaultsToIntegrationWorkflows()
    {
        var options = new PipelineOptions();

        Assert.That(options.TemporalTaskQueue, Is.EqualTo("integration-workflows"));
    }

    [Test]
    public void WorkflowTimeout_DefaultsToFiveMinutes()
    {
        var options = new PipelineOptions();

        Assert.That(options.WorkflowTimeout, Is.EqualTo(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public void SectionName_IsConstant()
    {
        Assert.That(PipelineOptions.SectionName, Is.EqualTo("Pipeline"));
    }

    [Test]
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

        Assert.That(options.NatsUrl, Is.EqualTo("nats://custom:4222"));
        Assert.That(options.InboundSubject, Is.EqualTo("custom.inbound"));
        Assert.That(options.AckSubject, Is.EqualTo("custom.ack"));
        Assert.That(options.NackSubject, Is.EqualTo("custom.nack"));
        Assert.That(options.ConsumerGroup, Is.EqualTo("custom-group"));
        Assert.That(options.TemporalServerAddress, Is.EqualTo("temporal:7233"));
        Assert.That(options.TemporalNamespace, Is.EqualTo("production"));
        Assert.That(options.TemporalTaskQueue, Is.EqualTo("production-workflows"));
        Assert.That(options.WorkflowTimeout, Is.EqualTo(TimeSpan.FromMinutes(10)));
    }
}
