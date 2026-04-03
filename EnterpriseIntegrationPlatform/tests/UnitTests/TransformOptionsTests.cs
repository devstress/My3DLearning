using EnterpriseIntegrationPlatform.Processing.Transform;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class TransformOptionsTests
{
    [Test]
    public void Defaults_EnabledIsTrue()
    {
        var options = new TransformOptions();
        Assert.That(options.Enabled, Is.True);
    }

    [Test]
    public void Defaults_MaxPayloadSizeBytesIsZero()
    {
        var options = new TransformOptions();
        Assert.That(options.MaxPayloadSizeBytes, Is.EqualTo(0));
    }

    [Test]
    public void Defaults_StopOnStepFailureIsTrue()
    {
        var options = new TransformOptions();
        Assert.That(options.StopOnStepFailure, Is.True);
    }

    [Test]
    public void SetEnabled_Roundtrips()
    {
        var options = new TransformOptions { Enabled = false };
        Assert.That(options.Enabled, Is.False);
    }

    [Test]
    public void SetMaxPayloadSizeBytes_Roundtrips()
    {
        var options = new TransformOptions { MaxPayloadSizeBytes = 1024 };
        Assert.That(options.MaxPayloadSizeBytes, Is.EqualTo(1024));
    }

    [Test]
    public void SetStopOnStepFailure_Roundtrips()
    {
        var options = new TransformOptions { StopOnStepFailure = false };
        Assert.That(options.StopOnStepFailure, Is.False);
    }
}
