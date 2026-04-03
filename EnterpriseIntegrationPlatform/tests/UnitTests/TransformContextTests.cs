using EnterpriseIntegrationPlatform.Processing.Transform;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class TransformContextTests
{
    [Test]
    public void Constructor_SetsPayloadAndContentType()
    {
        var ctx = new TransformContext("data", "text/plain");

        Assert.That(ctx.Payload, Is.EqualTo("data"));
        Assert.That(ctx.ContentType, Is.EqualTo("text/plain"));
    }

    [Test]
    public void Constructor_NullPayload_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TransformContext(null!, "text/plain"));
    }

    [Test]
    public void Constructor_EmptyContentType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TransformContext("data", ""));
    }

    [Test]
    public void WithPayloadAndContentType_CreatesNewContext()
    {
        var original = new TransformContext("old", "text/plain");
        original.Metadata["key"] = "value";

        var updated = original.WithPayload("new", "application/json");

        Assert.That(updated.Payload, Is.EqualTo("new"));
        Assert.That(updated.ContentType, Is.EqualTo("application/json"));
        Assert.That(updated.Metadata["key"], Is.EqualTo("value"));
    }

    [Test]
    public void WithPayloadOnly_PreservesContentType()
    {
        var original = new TransformContext("old", "application/xml");

        var updated = original.WithPayload("new");

        Assert.That(updated.Payload, Is.EqualTo("new"));
        Assert.That(updated.ContentType, Is.EqualTo("application/xml"));
    }

    [Test]
    public void Metadata_DefaultsToEmpty()
    {
        var ctx = new TransformContext("data", "text/plain");
        Assert.That(ctx.Metadata, Is.Not.Null);
        Assert.That(ctx.Metadata.Count, Is.EqualTo(0));
    }
}
