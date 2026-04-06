// ============================================================================
// Tutorial 17 – Normalizer (Lab)
// ============================================================================
// This lab exercises the MessageNormalizer — the pattern that detects the
// incoming payload format (JSON, XML, CSV) and converts it to canonical
// JSON. You will test format detection, JSON passthrough, XML-to-JSON
// conversion, CSV-to-JSON conversion, and strict content-type enforcement.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial17;

[TestFixture]
public sealed class Lab
{
    // ── JSON Passthrough ────────────────────────────────────────────────────

    [Test]
    public async Task Normalize_JsonPayload_PassesThroughUnchanged()
    {
        var options = Options.Create(new NormalizerOptions());
        var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

        var json = """{"name":"Alice","age":30}""";

        var result = await normalizer.NormalizeAsync(json, "application/json");

        Assert.That(result.DetectedFormat, Is.EqualTo("JSON"));
        Assert.That(result.WasTransformed, Is.False);
        Assert.That(result.OriginalContentType, Is.EqualTo("application/json"));

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.GetProperty("name").GetString(), Is.EqualTo("Alice"));
    }

    // ── XML to JSON Conversion ──────────────────────────────────────────────

    [Test]
    public async Task Normalize_XmlPayload_ConvertsToJson()
    {
        var options = Options.Create(new NormalizerOptions());
        var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

        var xml = "<Order><id>ORD-1</id><total>99.50</total></Order>";

        var result = await normalizer.NormalizeAsync(xml, "application/xml");

        Assert.That(result.DetectedFormat, Is.EqualTo("XML"));
        Assert.That(result.WasTransformed, Is.True);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.GetProperty("id").GetString(), Is.EqualTo("ORD-1"));
        Assert.That(doc.RootElement.GetProperty("total").GetString(), Is.EqualTo("99.50"));
    }

    // ── CSV to JSON Conversion ──────────────────────────────────────────────

    [Test]
    public async Task Normalize_CsvPayload_ConvertsToJsonArray()
    {
        var options = Options.Create(new NormalizerOptions());
        var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

        var csv = "name,age\nAlice,30\nBob,25";

        var result = await normalizer.NormalizeAsync(csv, "text/csv");

        Assert.That(result.DetectedFormat, Is.EqualTo("CSV"));
        Assert.That(result.WasTransformed, Is.True);

        using var doc = JsonDocument.Parse(result.Payload);
        var array = doc.RootElement.GetProperty("Root");
        Assert.That(array.GetArrayLength(), Is.EqualTo(2));
        Assert.That(array[0].GetProperty("name").GetString(), Is.EqualTo("Alice"));
        Assert.That(array[1].GetProperty("name").GetString(), Is.EqualTo("Bob"));
    }

    // ── Strict Content Type Enforcement ─────────────────────────────────────

    [Test]
    public void Normalize_UnknownContentType_StrictMode_Throws()
    {
        var options = Options.Create(new NormalizerOptions { StrictContentType = true });
        var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => normalizer.NormalizeAsync("{}", "application/octet-stream"));
    }

    // ── Best-Effort Detection (Non-Strict) ──────────────────────────────────

    [Test]
    public async Task Normalize_UnknownContentType_NonStrict_DetectsJson()
    {
        var options = Options.Create(new NormalizerOptions { StrictContentType = false });
        var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

        var json = """{"key":"value"}""";

        var result = await normalizer.NormalizeAsync(json, "application/octet-stream");

        Assert.That(result.DetectedFormat, Is.EqualTo("JSON"));
        Assert.That(result.WasTransformed, Is.False);
    }

    [Test]
    public async Task Normalize_UnknownContentType_NonStrict_DetectsXml()
    {
        var options = Options.Create(new NormalizerOptions { StrictContentType = false });
        var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

        var xml = "<Root><value>42</value></Root>";

        var result = await normalizer.NormalizeAsync(xml, "application/octet-stream");

        Assert.That(result.DetectedFormat, Is.EqualTo("XML"));
        Assert.That(result.WasTransformed, Is.True);
    }

    // ── Custom CSV Delimiter ────────────────────────────────────────────────

    [Test]
    public async Task Normalize_CsvWithCustomDelimiter_ParsesCorrectly()
    {
        var options = Options.Create(new NormalizerOptions { CsvDelimiter = ';' });
        var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

        var csv = "name;age\nAlice;30";

        var result = await normalizer.NormalizeAsync(csv, "text/csv");

        Assert.That(result.DetectedFormat, Is.EqualTo("CSV"));
        Assert.That(result.WasTransformed, Is.True);

        using var doc = JsonDocument.Parse(result.Payload);
        var array = doc.RootElement.GetProperty("Root");
        Assert.That(array[0].GetProperty("name").GetString(), Is.EqualTo("Alice"));
        Assert.That(array[0].GetProperty("age").GetString(), Is.EqualTo("30"));
    }
}
