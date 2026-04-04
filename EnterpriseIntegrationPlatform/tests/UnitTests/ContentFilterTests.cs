using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace UnitTests;

[TestFixture]
public sealed class ContentFilterTests
{
    private ContentFilter _filter = null!;

    [SetUp]
    public void SetUp()
    {
        _filter = new ContentFilter(NullLogger<ContentFilter>.Instance);
    }

    [Test]
    public async Task FilterAsync_SingleTopLevelField_RetainsOnlyThatField()
    {
        var payload = """{"name":"Alice","age":30,"email":"a@b.com"}""";
        var result = await _filter.FilterAsync(payload, ["name"]);

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("name").GetString(), Is.EqualTo("Alice"));
        Assert.That(doc.RootElement.TryGetProperty("age", out _), Is.False);
        Assert.That(doc.RootElement.TryGetProperty("email", out _), Is.False);
    }

    [Test]
    public async Task FilterAsync_MultipleFields_RetainsAllSpecified()
    {
        var payload = """{"name":"Alice","age":30,"email":"a@b.com","city":"NYC"}""";
        var result = await _filter.FilterAsync(payload, ["name", "email"]);

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("name").GetString(), Is.EqualTo("Alice"));
        Assert.That(doc.RootElement.GetProperty("email").GetString(), Is.EqualTo("a@b.com"));
        Assert.That(doc.RootElement.TryGetProperty("age", out _), Is.False);
        Assert.That(doc.RootElement.TryGetProperty("city", out _), Is.False);
    }

    [Test]
    public async Task FilterAsync_NestedPath_RetainsNestedField()
    {
        var payload = """{"order":{"id":1,"items":[1,2,3]},"customer":{"name":"Alice","address":{"city":"NYC","zip":"10001"}}}""";
        var result = await _filter.FilterAsync(payload, ["customer.address.city"]);

        using var doc = JsonDocument.Parse(result);
        Assert.That(
            doc.RootElement.GetProperty("customer").GetProperty("address").GetProperty("city").GetString(),
            Is.EqualTo("NYC"));
        Assert.That(doc.RootElement.TryGetProperty("order", out _), Is.False);
    }

    [Test]
    public async Task FilterAsync_MixedTopLevelAndNested_RetainsBoth()
    {
        var payload = """{"id":42,"meta":{"source":"test","version":"1.0"},"data":"payload"}""";
        var result = await _filter.FilterAsync(payload, ["id", "meta.version"]);

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("id").GetInt32(), Is.EqualTo(42));
        Assert.That(doc.RootElement.GetProperty("meta").GetProperty("version").GetString(),
            Is.EqualTo("1.0"));
        Assert.That(doc.RootElement.TryGetProperty("data", out _), Is.False);
    }

    [Test]
    public async Task FilterAsync_MissingPath_SilentlySkipped()
    {
        var payload = """{"name":"Alice"}""";
        var result = await _filter.FilterAsync(payload, ["name", "nonexistent"]);

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("name").GetString(), Is.EqualTo("Alice"));
        Assert.That(doc.RootElement.EnumerateObject().Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task FilterAsync_ArrayValuePreserved()
    {
        var payload = """{"items":[1,2,3],"name":"test"}""";
        var result = await _filter.FilterAsync(payload, ["items"]);

        using var doc = JsonDocument.Parse(result);
        var items = doc.RootElement.GetProperty("items");
        Assert.That(items.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(items.GetArrayLength(), Is.EqualTo(3));
    }

    [Test]
    public async Task FilterAsync_BooleanAndNullValues_Preserved()
    {
        var payload = """{"active":true,"deleted":false,"name":"test"}""";
        var result = await _filter.FilterAsync(payload, ["active", "deleted"]);

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("active").GetBoolean(), Is.True);
        Assert.That(doc.RootElement.GetProperty("deleted").GetBoolean(), Is.False);
    }

    [Test]
    public void FilterAsync_EmptyKeepPaths_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            () => _filter.FilterAsync("""{"a":1}""", []));
    }

    [Test]
    public void FilterAsync_NullPayload_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            () => _filter.FilterAsync(null!, ["a"]));
    }

    [Test]
    public void FilterAsync_NullKeepPaths_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            () => _filter.FilterAsync("""{"a":1}""", null!));
    }

    [Test]
    public void FilterAsync_NonObjectPayload_Throws()
    {
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _filter.FilterAsync("[1,2,3]", ["a"]));
    }

    [Test]
    public async Task FilterAsync_CancellationRequested_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Assert.ThrowsAsync<OperationCanceledException>(
            () => _filter.FilterAsync("""{"a":1}""", ["a"], cts.Token));
    }

    [Test]
    public async Task FilterAsync_DeeplyNestedPath_CreatesIntermediateObjects()
    {
        var payload = """{"a":{"b":{"c":{"d":"deep"}},"x":"skip"}}""";
        var result = await _filter.FilterAsync(payload, ["a.b.c.d"]);

        using var doc = JsonDocument.Parse(result);
        Assert.That(
            doc.RootElement
                .GetProperty("a")
                .GetProperty("b")
                .GetProperty("c")
                .GetProperty("d")
                .GetString(),
            Is.EqualTo("deep"));
    }

    [Test]
    public async Task FilterAsync_ObjectValuePreserved()
    {
        var payload = """{"config":{"retry":{"max":3,"delay":100}},"name":"svc"}""";
        var result = await _filter.FilterAsync(payload, ["config.retry"]);

        using var doc = JsonDocument.Parse(result);
        var retry = doc.RootElement.GetProperty("config").GetProperty("retry");
        Assert.That(retry.GetProperty("max").GetInt32(), Is.EqualTo(3));
        Assert.That(retry.GetProperty("delay").GetInt32(), Is.EqualTo(100));
    }
}
