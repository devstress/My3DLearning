using EnterpriseIntegrationPlatform.Security;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class InputSanitizerTests
{
    private InputSanitizer _sanitizer = null!;

    [SetUp]
    public void SetUp()
    {
        _sanitizer = new();
    }

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
        Assert.That(result.Contains('\0'), Is.False);
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

    // ───── XSS Detection ─────

    [Test]
    public void Sanitize_ScriptTag_Removed()
    {
        var result = _sanitizer.Sanitize("Hello<script>alert('xss')</script>World");
        Assert.That(result, Does.Not.Contain("<script>"));
        Assert.That(result, Does.Contain("Hello"));
        Assert.That(result, Does.Contain("World"));
    }

    [Test]
    public void IsClean_ScriptTag_ReturnsFalse()
    {
        Assert.That(_sanitizer.IsClean("Hello<script>alert('xss')</script>World"), Is.False);
    }

    [Test]
    public void Sanitize_InlineEventHandler_Removed()
    {
        var result = _sanitizer.Sanitize("img onclick= onerror= src=x");
        Assert.That(result, Does.Not.Contain("onclick="));
        Assert.That(result, Does.Not.Contain("onerror="));
    }

    [Test]
    public void IsClean_InlineEventHandler_ReturnsFalse()
    {
        Assert.That(_sanitizer.IsClean("img onclick=alert(1)"), Is.False);
    }

    // ───── SQL Injection Detection ─────

    [Test]
    public void Sanitize_SqlDropTable_Removed()
    {
        var result = _sanitizer.Sanitize("'; DROP TABLE users");
        Assert.That(result, Does.Not.Contain("DROP TABLE"));
    }

    [Test]
    public void Sanitize_SqlOrOneEqualsOne_Removed()
    {
        var result = _sanitizer.Sanitize("admin' OR 1=1 --");
        Assert.That(result, Does.Not.Contain("OR 1=1"));
    }

    [Test]
    public void Sanitize_SqlUnionSelect_Removed()
    {
        var result = _sanitizer.Sanitize("1 UNION SELECT * FROM users");
        Assert.That(result, Does.Not.Contain("UNION SELECT"));
    }

    [Test]
    public void IsClean_SqlInjection_ReturnsFalse()
    {
        Assert.That(_sanitizer.IsClean("'; DROP TABLE users"), Is.False);
        Assert.That(_sanitizer.IsClean(" OR 1=1"), Is.False);
        Assert.That(_sanitizer.IsClean("1 UNION SELECT *"), Is.False);
    }

    // ───── HTML Entity Detection ─────

    [Test]
    public void Sanitize_HtmlEntities_DecodedAndNeutralized()
    {
        var result = _sanitizer.Sanitize("&#60;script&#62;alert(1)&#60;/script&#62;");
        Assert.That(result, Does.Not.Contain("<script>"));
    }

    [Test]
    public void IsClean_HtmlEntity_ReturnsFalse()
    {
        Assert.That(_sanitizer.IsClean("&#60;script&#62;"), Is.False);
        Assert.That(_sanitizer.IsClean("&lt;script&gt;"), Is.False);
    }

    // ───── Unicode Direction Override Detection ─────

    [Test]
    public void Sanitize_UnicodeOverrides_Removed()
    {
        var result = _sanitizer.Sanitize("Hello\u202Eevil\u202CWorld");
        Assert.That(result.IndexOf('\u202E'), Is.EqualTo(-1), "U+202E should be removed");
        Assert.That(result.IndexOf('\u202C'), Is.EqualTo(-1), "U+202C should be removed");
        Assert.That(result, Is.EqualTo("HelloevilWorld"));
    }

    [Test]
    public void IsClean_UnicodeOverride_ReturnsFalse()
    {
        Assert.That(_sanitizer.IsClean("Hello\u202Eevil"), Is.False);
        Assert.That(_sanitizer.IsClean("Hello\u2066evil"), Is.False);
    }

    [Test]
    public void Sanitize_CleanInput_PassesThroughUnchanged()
    {
        var result = _sanitizer.Sanitize("Perfectly normal text");
        Assert.That(result, Is.EqualTo("Perfectly normal text"));
    }
}
