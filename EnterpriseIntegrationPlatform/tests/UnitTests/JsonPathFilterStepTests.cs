using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Transform;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class JsonPathFilterStepTests
{
    [Test]
    public async Task ExecuteAsync_SinglePath_ExtractsOnlyThatProperty()
    {
        var step = new JsonPathFilterStep(["name"]);
        var context = new TransformContext(
            """{"name":"Alice","age":30,"email":"a@b.com"}""", "application/json");

        var result = await step.ExecuteAsync(context);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.TryGetProperty("name", out var name), Is.True);
        Assert.That(name.GetString(), Is.EqualTo("Alice"));
        Assert.That(doc.RootElement.TryGetProperty("age", out _), Is.False);
        Assert.That(doc.RootElement.TryGetProperty("email", out _), Is.False);
    }

    [Test]
    public async Task ExecuteAsync_MultiplePaths_ExtractsAll()
    {
        var step = new JsonPathFilterStep(["name", "age"]);
        var context = new TransformContext(
            """{"name":"Alice","age":30,"email":"a@b.com"}""", "application/json");

        var result = await step.ExecuteAsync(context);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.TryGetProperty("name", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("age", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("email", out _), Is.False);
    }

    [Test]
    public async Task ExecuteAsync_NestedPath_CreatesIntermediateObjects()
    {
        var step = new JsonPathFilterStep(["order.id"]);
        var context = new TransformContext(
            """{"order":{"id":42,"total":100},"customer":"Bob"}""", "application/json");

        var result = await step.ExecuteAsync(context);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.TryGetProperty("order", out var order), Is.True);
        Assert.That(order.TryGetProperty("id", out var id), Is.True);
        Assert.That(id.GetInt32(), Is.EqualTo(42));
        Assert.That(doc.RootElement.TryGetProperty("customer", out _), Is.False);
    }

    [Test]
    public async Task ExecuteAsync_MissingPath_SilentlySkipped()
    {
        var step = new JsonPathFilterStep(["nonexistent"]);
        var context = new TransformContext("""{"name":"Alice"}""", "application/json");

        var result = await step.ExecuteAsync(context);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.EnumerateObject().Any(), Is.False);
    }

    [Test]
    public async Task ExecuteAsync_PreservesArrayValues()
    {
        var step = new JsonPathFilterStep(["tags"]);
        var context = new TransformContext(
            """{"tags":["a","b"],"name":"test"}""", "application/json");

        var result = await step.ExecuteAsync(context);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.TryGetProperty("tags", out var tags), Is.True);
        Assert.That(tags.GetArrayLength(), Is.EqualTo(2));
    }

    [Test]
    public async Task ExecuteAsync_SetsMetadata()
    {
        var step = new JsonPathFilterStep(["a"]);
        var context = new TransformContext("""{"a":1}""", "application/json");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Metadata.ContainsKey("Step.JsonPathFilter.Applied"), Is.True);
    }

    [Test]
    public async Task ExecuteAsync_ContentTypeIsJson()
    {
        var step = new JsonPathFilterStep(["a"]);
        var context = new TransformContext("""{"a":1}""", "application/json");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.ContentType, Is.EqualTo("application/json"));
    }

    [Test]
    public void Name_ReturnsJsonPathFilter()
    {
        var step = new JsonPathFilterStep(["a"]);
        Assert.That(step.Name, Is.EqualTo("JsonPathFilter"));
    }

    [Test]
    public void Constructor_EmptyPaths_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new JsonPathFilterStep([]));
    }

    [Test]
    public void Constructor_NullPaths_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonPathFilterStep(null!));
    }

    [Test]
    public void ExecuteAsync_NullContext_ThrowsArgumentNullException()
    {
        var step = new JsonPathFilterStep(["a"]);
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await step.ExecuteAsync(null!));
    }
}
