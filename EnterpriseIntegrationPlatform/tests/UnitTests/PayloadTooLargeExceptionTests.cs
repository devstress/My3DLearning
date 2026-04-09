using EnterpriseIntegrationPlatform.Security;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PayloadTooLargeExceptionTests
{
    [Test]
    public void Constructor_SetsActualBytes()
    {
        var ex = new PayloadTooLargeException(2000, 1000);
        Assert.That(ex.ActualBytes, Is.EqualTo(2000));
    }

    [Test]
    public void Constructor_SetsMaxBytes()
    {
        var ex = new PayloadTooLargeException(2000, 1000);
        Assert.That(ex.MaxBytes, Is.EqualTo(1000));
    }

    [Test]
    public void Constructor_MessageContainsBothValues()
    {
        var ex = new PayloadTooLargeException(2000, 1000);
        Assert.That(ex.Message, Does.Contain("2000"));
        Assert.That(ex.Message, Does.Contain("1000"));
    }

    [Test]
    public void IsException_DerivesFromException()
    {
        var ex = new PayloadTooLargeException(100, 50);
        Assert.That(ex, Is.InstanceOf<Exception>());
    }
}
