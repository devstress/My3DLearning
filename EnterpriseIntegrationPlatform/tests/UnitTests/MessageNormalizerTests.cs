using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace UnitTests;

[TestFixture]
public sealed class MessageNormalizerTests
{
    private MessageNormalizer _normalizer = null!;
    private NormalizerOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new NormalizerOptions();
        _normalizer = new MessageNormalizer(
            Options.Create(_options),
            NullLogger<MessageNormalizer>.Instance);
    }

    [Test]
    public async Task NormalizeAsync_JsonPayload_PassesThrough()
    {
        var payload = """{"name":"Alice","age":30}""";
        var result = await _normalizer.NormalizeAsync(payload, "application/json");

        Assert.That(result.DetectedFormat, Is.EqualTo("JSON"));
        Assert.That(result.WasTransformed, Is.False);
        Assert.That(result.OriginalContentType, Is.EqualTo("application/json"));

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.GetProperty("name").GetString(), Is.EqualTo("Alice"));
    }

    [Test]
    public async Task NormalizeAsync_XmlPayload_ConvertsToJson()
    {
        var xml = """<root><name>Alice</name><age>30</age></root>""";
        var result = await _normalizer.NormalizeAsync(xml, "application/xml");

        Assert.That(result.DetectedFormat, Is.EqualTo("XML"));
        Assert.That(result.WasTransformed, Is.True);
        Assert.That(result.OriginalContentType, Is.EqualTo("application/xml"));

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.GetProperty("name").GetString(), Is.EqualTo("Alice"));
        Assert.That(doc.RootElement.GetProperty("age").GetString(), Is.EqualTo("30"));
    }

    [Test]
    public async Task NormalizeAsync_XmlWithNestedElements_ConvertsCorrectly()
    {
        var xml = """<root><order><id>42</id><item>Widget</item></order></root>""";
        var result = await _normalizer.NormalizeAsync(xml, "text/xml");

        using var doc = JsonDocument.Parse(result.Payload);
        var order = doc.RootElement.GetProperty("order");
        Assert.That(order.GetProperty("id").GetString(), Is.EqualTo("42"));
        Assert.That(order.GetProperty("item").GetString(), Is.EqualTo("Widget"));
    }

    [Test]
    public async Task NormalizeAsync_XmlWithRepeatedElements_ProducesArray()
    {
        var xml = """<root><item>A</item><item>B</item><item>C</item></root>""";
        var result = await _normalizer.NormalizeAsync(xml, "application/xml");

        using var doc = JsonDocument.Parse(result.Payload);
        var items = doc.RootElement.GetProperty("item");
        Assert.That(items.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(items.GetArrayLength(), Is.EqualTo(3));
    }

    [Test]
    public async Task NormalizeAsync_CsvWithHeaders_ConvertsToJsonArray()
    {
        var csv = "name,age,city\nAlice,30,NYC\nBob,25,LA\n";
        var result = await _normalizer.NormalizeAsync(csv, "text/csv");

        Assert.That(result.DetectedFormat, Is.EqualTo("CSV"));
        Assert.That(result.WasTransformed, Is.True);

        using var doc = JsonDocument.Parse(result.Payload);
        var rows = doc.RootElement.GetProperty("Root");
        Assert.That(rows.GetArrayLength(), Is.EqualTo(2));
        Assert.That(rows[0].GetProperty("name").GetString(), Is.EqualTo("Alice"));
        Assert.That(rows[0].GetProperty("age").GetString(), Is.EqualTo("30"));
        Assert.That(rows[1].GetProperty("name").GetString(), Is.EqualTo("Bob"));
    }

    [Test]
    public async Task NormalizeAsync_CsvWithQuotedFields_ParsesCorrectly()
    {
        var csv = "name,description\nAlice,\"Has a, comma\"\nBob,\"Simple\"\n";
        var result = await _normalizer.NormalizeAsync(csv, "text/csv");

        using var doc = JsonDocument.Parse(result.Payload);
        var rows = doc.RootElement.GetProperty("Root");
        Assert.That(rows[0].GetProperty("description").GetString(), Is.EqualTo("Has a, comma"));
    }

    [Test]
    public async Task NormalizeAsync_CsvWithoutHeaders_ProducesArrayOfArrays()
    {
        _options = new NormalizerOptions { CsvHasHeaders = false };
        _normalizer = new MessageNormalizer(
            Options.Create(_options),
            NullLogger<MessageNormalizer>.Instance);

        var csv = "Alice,30,NYC\nBob,25,LA\n";
        var result = await _normalizer.NormalizeAsync(csv, "text/csv");

        using var doc = JsonDocument.Parse(result.Payload);
        var rows = doc.RootElement.GetProperty("Root");
        Assert.That(rows.GetArrayLength(), Is.EqualTo(2));
        Assert.That(rows[0][0].GetString(), Is.EqualTo("Alice"));
    }

    [Test]
    public void NormalizeAsync_UnknownContentType_StrictMode_Throws()
    {
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _normalizer.NormalizeAsync("data", "application/octet-stream"));
    }

    [Test]
    public async Task NormalizeAsync_UnknownContentType_NonStrictMode_DetectsJson()
    {
        _options = new NormalizerOptions { StrictContentType = false };
        _normalizer = new MessageNormalizer(
            Options.Create(_options),
            NullLogger<MessageNormalizer>.Instance);

        var payload = """{"detected":"json"}""";
        var result = await _normalizer.NormalizeAsync(payload, "application/octet-stream");

        Assert.That(result.DetectedFormat, Is.EqualTo("JSON"));
    }

    [Test]
    public async Task NormalizeAsync_UnknownContentType_NonStrictMode_DetectsXml()
    {
        _options = new NormalizerOptions { StrictContentType = false };
        _normalizer = new MessageNormalizer(
            Options.Create(_options),
            NullLogger<MessageNormalizer>.Instance);

        var payload = """<root><name>Test</name></root>""";
        var result = await _normalizer.NormalizeAsync(payload, "application/octet-stream");

        Assert.That(result.DetectedFormat, Is.EqualTo("XML"));
        Assert.That(result.WasTransformed, Is.True);
    }

    [Test]
    public void NormalizeAsync_NullPayload_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            () => _normalizer.NormalizeAsync(null!, "application/json"));
    }

    [Test]
    public void NormalizeAsync_EmptyContentType_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            () => _normalizer.NormalizeAsync("{}", " "));
    }

    [Test]
    public async Task NormalizeAsync_CancellationRequested_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Assert.ThrowsAsync<OperationCanceledException>(
            () => _normalizer.NormalizeAsync("{}", "application/json", cts.Token));
    }

    [Test]
    public async Task NormalizeAsync_JsonArray_PassesThrough()
    {
        var payload = """[1,2,3]""";
        var result = await _normalizer.NormalizeAsync(payload, "application/json");

        Assert.That(result.DetectedFormat, Is.EqualTo("JSON"));
        Assert.That(result.WasTransformed, Is.False);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
    }

    [Test]
    public async Task NormalizeAsync_CsvCustomDelimiter_ParsesCorrectly()
    {
        _options = new NormalizerOptions { CsvDelimiter = ';' };
        _normalizer = new MessageNormalizer(
            Options.Create(_options),
            NullLogger<MessageNormalizer>.Instance);

        var csv = "name;age\nAlice;30\n";
        var result = await _normalizer.NormalizeAsync(csv, "text/csv");

        using var doc = JsonDocument.Parse(result.Payload);
        var rows = doc.RootElement.GetProperty("Root");
        Assert.That(rows[0].GetProperty("name").GetString(), Is.EqualTo("Alice"));
        Assert.That(rows[0].GetProperty("age").GetString(), Is.EqualTo("30"));
    }

    [Test]
    public async Task NormalizeAsync_ContentTypeWithCharset_DetectsCorrectly()
    {
        var payload = """{"name":"Alice"}""";
        var result = await _normalizer.NormalizeAsync(payload, "application/json; charset=utf-8");

        Assert.That(result.DetectedFormat, Is.EqualTo("JSON"));
    }

    [Test]
    public async Task NormalizeAsync_CsvWithCustomXmlRootName_UsesConfiguredName()
    {
        var normalizer = new MessageNormalizer(
            Options.Create(new NormalizerOptions { XmlRootName = "records" }),
            NullLogger<MessageNormalizer>.Instance);

        var csv = "name,age\nAlice,30\n";
        var result = await normalizer.NormalizeAsync(csv, "text/csv");

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.TryGetProperty("records", out var records), Is.True);
        Assert.That(records.GetArrayLength(), Is.EqualTo(1));
    }
}
