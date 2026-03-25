using EnterpriseIntegrationPlatform.Security;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class InputSanitizerTests
{
    private readonly InputSanitizer _sanitizer = new();

    [Fact]
    public void Sanitize_CleanInput_ReturnsUnchanged()
    {
        var result = _sanitizer.Sanitize("hello world");
        result.Should().Be("hello world");
    }

    [Fact]
    public void Sanitize_CrlfInjection_RemovesCrLf()
    {
        var result = _sanitizer.Sanitize("line1\r\nline2");
        result.Should().NotContain("\r").And.NotContain("\n");
    }

    [Fact]
    public void Sanitize_NullBytes_RemovesNullBytes()
    {
        var result = _sanitizer.Sanitize("hello\0world");
        result.Should().NotContain("\0");
    }

    [Fact]
    public void Sanitize_LeadingTrailingWhitespace_Trimmed()
    {
        var result = _sanitizer.Sanitize("  hello  ");
        result.Should().Be("hello");
    }

    [Fact]
    public void Sanitize_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sanitizer.Sanitize(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsClean_CleanInput_ReturnsTrue()
    {
        _sanitizer.IsClean("safe string").Should().BeTrue();
    }

    [Fact]
    public void IsClean_InputWithNewline_ReturnsFalse()
    {
        _sanitizer.IsClean("bad\ninput").Should().BeFalse();
    }

    [Fact]
    public void IsClean_InputWithCarriageReturn_ReturnsFalse()
    {
        _sanitizer.IsClean("bad\rinput").Should().BeFalse();
    }

    [Fact]
    public void IsClean_InputWithNullByte_ReturnsFalse()
    {
        _sanitizer.IsClean("bad\0input").Should().BeFalse();
    }
}
