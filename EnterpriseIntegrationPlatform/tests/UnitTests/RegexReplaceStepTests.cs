using EnterpriseIntegrationPlatform.Processing.Transform;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class RegexReplaceStepTests
{
    [Test]
    public async Task ExecuteAsync_SimpleReplacement_ReplacesAll()
    {
        var step = new RegexReplaceStep(@"\bfoo\b", "bar");
        var context = new TransformContext("foo is foo", "text/plain");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Payload, Is.EqualTo("bar is bar"));
    }

    [Test]
    public async Task ExecuteAsync_NoMatch_ReturnsUnchanged()
    {
        var step = new RegexReplaceStep("xyz", "abc");
        var context = new TransformContext("hello world", "text/plain");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Payload, Is.EqualTo("hello world"));
    }

    [Test]
    public async Task ExecuteAsync_WithCaptureGroup_SubstitutesCorrectly()
    {
        var step = new RegexReplaceStep(@"(\w+)@(\w+)", "$2/$1");
        var context = new TransformContext("user@domain", "text/plain");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Payload, Is.EqualTo("domain/user"));
    }

    [Test]
    public async Task ExecuteAsync_CaseInsensitive_Matches()
    {
        var step = new RegexReplaceStep("hello", "HI",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var context = new TransformContext("HELLO World", "text/plain");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Payload, Is.EqualTo("HI World"));
    }

    [Test]
    public async Task ExecuteAsync_PreservesContentType()
    {
        var step = new RegexReplaceStep("a", "b");
        var context = new TransformContext("aaa", "application/json");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.ContentType, Is.EqualTo("application/json"));
    }

    [Test]
    public async Task ExecuteAsync_SetsMetadata()
    {
        var step = new RegexReplaceStep("a", "b");
        var context = new TransformContext("abc", "text/plain");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Metadata.ContainsKey("Step.RegexReplace.Applied"), Is.True);
    }

    [Test]
    public void Name_ReturnsRegexReplace()
    {
        var step = new RegexReplaceStep("a", "b");
        Assert.That(step.Name, Is.EqualTo("RegexReplace"));
    }

    [Test]
    public void Constructor_EmptyPattern_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RegexReplaceStep("", "b"));
    }

    [Test]
    public void Constructor_NullReplacement_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RegexReplaceStep("a", null!));
    }

    [Test]
    public void ExecuteAsync_NullContext_ThrowsArgumentNullException()
    {
        var step = new RegexReplaceStep("a", "b");
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await step.ExecuteAsync(null!));
    }
}
