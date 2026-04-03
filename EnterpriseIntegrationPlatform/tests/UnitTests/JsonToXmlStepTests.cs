using EnterpriseIntegrationPlatform.Processing.Transform;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class JsonToXmlStepTests
{
    [Test]
    public async Task ExecuteAsync_SimpleObject_ProducesValidXml()
    {
        var step = new JsonToXmlStep("Order");
        var context = new TransformContext(
            """{"id":1,"name":"Widget"}""", "application/json");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.ContentType, Is.EqualTo("application/xml"));
        Assert.That(result.Payload, Does.Contain("<Order>"));
        Assert.That(result.Payload, Does.Contain("<id>1</id>"));
        Assert.That(result.Payload, Does.Contain("<name>Widget</name>"));
    }

    [Test]
    public async Task ExecuteAsync_NestedObject_ProducesNestedXml()
    {
        var step = new JsonToXmlStep();
        var context = new TransformContext(
            """{"customer":{"name":"Alice","city":"London"}}""", "application/json");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Payload, Does.Contain("<customer>"));
        Assert.That(result.Payload, Does.Contain("<name>Alice</name>"));
        Assert.That(result.Payload, Does.Contain("<city>London</city>"));
    }

    [Test]
    public async Task ExecuteAsync_Array_ProducesRepeatedItemElements()
    {
        var step = new JsonToXmlStep();
        var context = new TransformContext(
            """{"items":[1,2,3]}""", "application/json");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Payload, Does.Contain("<items>"));
        Assert.That(result.Payload, Does.Contain("<Item>1</Item>"));
        Assert.That(result.Payload, Does.Contain("<Item>2</Item>"));
        Assert.That(result.Payload, Does.Contain("<Item>3</Item>"));
    }

    [Test]
    public async Task ExecuteAsync_BooleanValues_ConvertsToStrings()
    {
        var step = new JsonToXmlStep();
        var context = new TransformContext(
            """{"active":true,"deleted":false}""", "application/json");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Payload, Does.Contain("<active>true</active>"));
        Assert.That(result.Payload, Does.Contain("<deleted>false</deleted>"));
    }

    [Test]
    public async Task ExecuteAsync_SetsMetadata()
    {
        var step = new JsonToXmlStep();
        var context = new TransformContext("""{"a":1}""", "application/json");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Metadata.ContainsKey("Step.JsonToXml.Applied"), Is.True);
    }

    [Test]
    public void Name_ReturnsJsonToXml()
    {
        var step = new JsonToXmlStep();
        Assert.That(step.Name, Is.EqualTo("JsonToXml"));
    }

    [Test]
    public void Constructor_EmptyRootName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new JsonToXmlStep(""));
    }

    [Test]
    public void ExecuteAsync_NullContext_ThrowsArgumentNullException()
    {
        var step = new JsonToXmlStep();
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await step.ExecuteAsync(null!));
    }

    [Test]
    public async Task ExecuteAsync_CustomRootElementName_UsesIt()
    {
        var step = new JsonToXmlStep("Invoice");
        var context = new TransformContext("""{"total":42}""", "application/json");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Payload, Does.Contain("<Invoice>"));
    }
}
