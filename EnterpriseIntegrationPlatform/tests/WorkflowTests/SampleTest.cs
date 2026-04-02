using NUnit.Framework;

using EnterpriseIntegrationPlatform.Workflow.Temporal;

namespace EnterpriseIntegrationPlatform.Tests.Workflow;

[TestFixture]
public class TemporalOptionsTests
{
    [Test]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new TemporalOptions();

        Assert.That(options.ServerAddress, Is.EqualTo("localhost:15233"));
        Assert.That(options.Namespace, Is.EqualTo("default"));
        Assert.That(options.TaskQueue, Is.EqualTo("integration-workflows"));
    }

    [Test]
    public void SectionName_ShouldBeTemporal()
    {
        Assert.That(TemporalOptions.SectionName, Is.EqualTo("Temporal"));
    }

    [Test]
    public void Properties_ShouldBeSettable()
    {
        var options = new TemporalOptions
        {
            ServerAddress = "temporal.prod:7233",
            Namespace = "production",
            TaskQueue = "custom-queue",
        };

        Assert.That(options.ServerAddress, Is.EqualTo("temporal.prod:7233"));
        Assert.That(options.Namespace, Is.EqualTo("production"));
        Assert.That(options.TaskQueue, Is.EqualTo("custom-queue"));
    }
}
