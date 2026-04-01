using EnterpriseIntegrationPlatform.Security;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class InputSanitizerTests
{
    private readonly InputSanitizer _sanitizer = new();

    [Test]
    public void Sanitize_CleanInput_ReturnsUnchanged()
    {
        var result = _sanitizer.Sanitize("hello world");
        Assert.That(result, Is.EqualTo("hello world"));
    }

    [Test]
    public void Sanitize_CrlfInjection_RemovesCrLf()
    {
        var result = _sanitizer.Sanitize("line1\r\nline2");
        Assert.That(result, Does.Not.Contain("\r").And.Not.Contain("\n"));
    }

    [Test]
    public void Sanitize_NullBytes_RemovesNullBytes()
    {
        var result = _sanitizer.Sanitize("hello\0world");
        Assert.That(result, Does.Not.Contain("\0"));
    }

    [Test]
    public void Sanitize_LeadingTrailingWhitespace_Trimmed()
    {
        var result = _sanitizer.Sanitize("  hello  ");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Sanitize_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sanitizer.Sanitize(null!);
        Assert.Throws<ArgumentNullException>(() => act());
    }

    [Test]
    public void IsClean_CleanInput_ReturnsTrue()
    {
        Assert.That(_sanitizer.IsClean("safe string"), Is.True);
    }

    [Test]
    public void IsClean_InputWithNewline_ReturnsFalse()
    {
        Assert.That(_sanitizer.IsClean("bad\ninput"), Is.False);
    }

    [Test]
    public void IsClean_InputWithCarriageReturn_ReturnsFalse()
    {
        Assert.That(_sanitizer.IsClean("bad\rinput"), Is.False);
    }

    [Test]
    public void IsClean_InputWithNullByte_ReturnsFalse()
    {
        Assert.That(_sanitizer.IsClean("bad\0input"), Is.False);
    }
}
