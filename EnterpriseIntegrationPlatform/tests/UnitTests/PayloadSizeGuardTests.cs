using EnterpriseIntegrationPlatform.Security;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class PayloadSizeGuardTests
{
    private static PayloadSizeGuard Build(int maxBytes = 100) =>
        new(Options.Create(new PayloadSizeOptions { MaxPayloadBytes = maxBytes }));

    [Fact]
    public void Enforce_PayloadWithinLimit_DoesNotThrow()
    {
        var guard = Build(100);
        var act = () => guard.Enforce("hello");
        act.Should().NotThrow();
    }

    [Fact]
    public void Enforce_PayloadExceedsLimit_ThrowsPayloadTooLargeException()
    {
        var guard = Build(5);
        var act = () => guard.Enforce("this is more than five bytes");
        act.Should().Throw<PayloadTooLargeException>();
    }

    [Fact]
    public void Enforce_ExactlyAtLimit_DoesNotThrow()
    {
        var guard = Build(5);
        var act = () => guard.Enforce("hello"); // exactly 5 UTF-8 bytes
        act.Should().NotThrow();
    }

    [Fact]
    public void Enforce_ByteArrayExceedsLimit_ThrowsPayloadTooLargeException()
    {
        var guard = Build(3);
        var act = () => guard.Enforce(new byte[] { 1, 2, 3, 4 });
        act.Should().Throw<PayloadTooLargeException>();
    }

    [Fact]
    public void Enforce_ByteArrayWithinLimit_DoesNotThrow()
    {
        var guard = Build(10);
        var act = () => guard.Enforce(new byte[] { 1, 2, 3 });
        act.Should().NotThrow();
    }

    [Fact]
    public void PayloadTooLargeException_ExposesActualAndMaxBytes()
    {
        var guard = Build(5);
        var act = () => guard.Enforce(new byte[10]);
        act.Should().Throw<PayloadTooLargeException>()
            .Which.ActualBytes.Should().Be(10);
    }

    [Fact]
    public void Enforce_NullString_ThrowsArgumentNullException()
    {
        var guard = Build();
        var act = () => guard.Enforce((string)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enforce_NullByteArray_ThrowsArgumentNullException()
    {
        var guard = Build();
        var act = () => guard.Enforce((byte[])null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
