using EnterpriseIntegrationPlatform.Security;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PayloadSizeOptionsTests
{
    [Test]
    public void SectionName_IsPayloadSize()
    {
        Assert.That(PayloadSizeOptions.SectionName, Is.EqualTo("PayloadSize"));
    }

    [Test]
    public void Default_MaxPayloadBytes_Is1MB()
    {
        var options = new PayloadSizeOptions();
        Assert.That(options.MaxPayloadBytes, Is.EqualTo(1_048_576));
    }

    [Test]
    public void MaxPayloadBytes_IsSettable()
    {
        var options = new PayloadSizeOptions { MaxPayloadBytes = 500 };
        Assert.That(options.MaxPayloadBytes, Is.EqualTo(500));
    }
}
