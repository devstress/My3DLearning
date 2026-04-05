using NUnit.Framework;

using EnterpriseIntegrationPlatform.Workflow.Temporal;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public sealed class TemporalOptionsTests
{
    [Test]
    public void SectionName_IsCorrect()
    {
        Assert.That(TemporalOptions.SectionName, Is.EqualTo("Temporal"));
    }

    [Test]
    public void Defaults_ServerAddress()
    {
        var options = new TemporalOptions();

        Assert.That(options.ServerAddress, Is.EqualTo("localhost:15233"));
    }

    [Test]
    public void Defaults_Namespace()
    {
        var options = new TemporalOptions();

        Assert.That(options.Namespace, Is.EqualTo("default"));
    }

    [Test]
    public void Defaults_TaskQueue()
    {
        var options = new TemporalOptions();

        Assert.That(options.TaskQueue, Is.EqualTo("integration-workflows"));
    }
}
