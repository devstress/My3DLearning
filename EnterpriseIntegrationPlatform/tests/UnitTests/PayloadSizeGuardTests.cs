using EnterpriseIntegrationPlatform.Security;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PayloadSizeGuardTests
{
    private static PayloadSizeGuard Build(int maxBytes = 100) =>
        new(Options.Create(new PayloadSizeOptions { MaxPayloadBytes = maxBytes }));

    [Test]
    public void Enforce_PayloadWithinLimit_DoesNotThrow()
    {
        var guard = Build(100);
        var act = () => guard.Enforce("hello");
        Assert.DoesNotThrow(() => act());
    }

    [Test]
    public void Enforce_PayloadExceedsLimit_ThrowsPayloadTooLargeException()
    {
        var guard = Build(5);
        var act = () => guard.Enforce("this is more than five bytes");
        Assert.Throws<PayloadTooLargeException>(() => act());
    }

    [Test]
    public void Enforce_ExactlyAtLimit_DoesNotThrow()
    {
        var guard = Build(5);
        var act = () => guard.Enforce("hello"); // exactly 5 UTF-8 bytes
        Assert.DoesNotThrow(() => act());
    }

    [Test]
    public void Enforce_ByteArrayExceedsLimit_ThrowsPayloadTooLargeException()
    {
        var guard = Build(3);
        var act = () => guard.Enforce(new byte[] { 1, 2, 3, 4 });
        Assert.Throws<PayloadTooLargeException>(() => act());
    }

    [Test]
    public void Enforce_ByteArrayWithinLimit_DoesNotThrow()
    {
        var guard = Build(10);
        var act = () => guard.Enforce(new byte[] { 1, 2, 3 });
        Assert.DoesNotThrow(() => act());
    }

    [Test]
    public void PayloadTooLargeException_ExposesActualAndMaxBytes()
    {
        var guard = Build(5);
        var act = () => guard.Enforce(new byte[10]);
        var ex = Assert.Throws<PayloadTooLargeException>(() => act());
        Assert.That(ex!.ActualBytes, Is.EqualTo(10));
    }

    [Test]
    public void Enforce_NullString_ThrowsArgumentNullException()
    {
        var guard = Build();
        var act = () => guard.Enforce((string)null!);
        Assert.Throws<ArgumentNullException>(() => act());
    }

    [Test]
    public void Enforce_NullByteArray_ThrowsArgumentNullException()
    {
        var guard = Build();
        var act = () => guard.Enforce((byte[])null!);
        Assert.Throws<ArgumentNullException>(() => act());
    }
}
