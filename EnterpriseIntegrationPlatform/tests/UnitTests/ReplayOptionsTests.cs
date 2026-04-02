using EnterpriseIntegrationPlatform.Processing.Replay;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class ReplayOptionsTests
{
    [Test]
    public void MaxMessages_Default_Is1000()
    {
        var options = new ReplayOptions();
        Assert.That(options.MaxMessages, Is.EqualTo(1000));
    }

    [Test]
    public void BatchSize_Default_Is100()
    {
        var options = new ReplayOptions();
        Assert.That(options.BatchSize, Is.EqualTo(100));
    }

    [Test]
    public void SourceTopic_Default_IsEmptyString()
    {
        var options = new ReplayOptions();
        Assert.That(options.SourceTopic, Is.Empty);
    }

    [Test]
    public void TargetTopic_Default_IsEmptyString()
    {
        var options = new ReplayOptions();
        Assert.That(options.TargetTopic, Is.Empty);
    }

    [Test]
    public void Properties_SetValues_ReturnCorrectValues()
    {
        var options = new ReplayOptions
        {
            SourceTopic = "events.source",
            TargetTopic = "events.target",
            MaxMessages = 500,
            BatchSize = 50
        };

        Assert.That(options.SourceTopic, Is.EqualTo("events.source"));
        Assert.That(options.TargetTopic, Is.EqualTo("events.target"));
        Assert.That(options.MaxMessages, Is.EqualTo(500));
        Assert.That(options.BatchSize, Is.EqualTo(50));
    }
}
