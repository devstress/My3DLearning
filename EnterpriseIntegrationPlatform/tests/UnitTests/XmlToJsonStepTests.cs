using EnterpriseIntegrationPlatform.Processing.Transform;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class XmlToJsonStepTests
{
    [Test]
    public async Task ExecuteAsync_SimpleXml_ProducesJson()
    {
        var step = new XmlToJsonStep();
        var xml = "<Order><id>1</id><name>Widget</name></Order>";
        var context = new TransformContext(xml, "application/xml");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.ContentType, Is.EqualTo("application/json"));
        Assert.That(result.Payload, Does.Contain("\"id\""));
        Assert.That(result.Payload, Does.Contain("\"1\""));
        Assert.That(result.Payload, Does.Contain("\"name\""));
        Assert.That(result.Payload, Does.Contain("\"Widget\""));
    }

    [Test]
    public async Task ExecuteAsync_NestedXml_ProducesNestedJson()
    {
        var step = new XmlToJsonStep();
        var xml = "<Root><customer><name>Alice</name></customer></Root>";
        var context = new TransformContext(xml, "application/xml");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Payload, Does.Contain("\"customer\""));
        Assert.That(result.Payload, Does.Contain("\"name\""));
        Assert.That(result.Payload, Does.Contain("\"Alice\""));
    }

    [Test]
    public async Task ExecuteAsync_RepeatedElements_ProducesJsonArray()
    {
        var step = new XmlToJsonStep();
        var xml = "<Root><item>A</item><item>B</item><item>C</item></Root>";
        var context = new TransformContext(xml, "application/xml");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Payload, Does.Contain("\"item\""));
        Assert.That(result.Payload, Does.Contain("["));
        Assert.That(result.Payload, Does.Contain("\"A\""));
        Assert.That(result.Payload, Does.Contain("\"B\""));
        Assert.That(result.Payload, Does.Contain("\"C\""));
    }

    [Test]
    public async Task ExecuteAsync_XmlWithAttributes_IncludesAttributesWithAtPrefix()
    {
        var step = new XmlToJsonStep();
        var xml = """<Root><item type="book"><name>Moby Dick</name></item></Root>""";
        var context = new TransformContext(xml, "application/xml");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Payload, Does.Contain("\"@type\""));
        Assert.That(result.Payload, Does.Contain("\"book\""));
    }

    [Test]
    public async Task ExecuteAsync_SetsMetadata()
    {
        var step = new XmlToJsonStep();
        var context = new TransformContext("<Root><a>1</a></Root>", "application/xml");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Metadata.ContainsKey("Step.XmlToJson.Applied"), Is.True);
    }

    [Test]
    public void Name_ReturnsXmlToJson()
    {
        var step = new XmlToJsonStep();
        Assert.That(step.Name, Is.EqualTo("XmlToJson"));
    }

    [Test]
    public void ExecuteAsync_NullContext_ThrowsArgumentNullException()
    {
        var step = new XmlToJsonStep();
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await step.ExecuteAsync(null!));
    }

    [Test]
    public void ExecuteAsync_InvalidXml_ThrowsException()
    {
        var step = new XmlToJsonStep();
        var context = new TransformContext("not-xml", "application/xml");

        Assert.ThrowsAsync<System.Xml.XmlException>(
            async () => await step.ExecuteAsync(context));
    }
}
