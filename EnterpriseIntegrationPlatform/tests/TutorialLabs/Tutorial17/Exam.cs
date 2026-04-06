// ============================================================================
// Tutorial 17 – Normalizer (Exam)
// ============================================================================
// Coding challenges: normalise a multi-format message stream, verify XML
// with nested elements, and test CSV-without-headers mode.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial17;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Multi-Format Stream ────────────────────────────────────

    [Test]
    public async Task Challenge1_MultiFormat_AllNormaliseToJson()
    {
        // Normalise three payloads (JSON, XML, CSV) using one normalizer and
        // verify they all produce valid JSON output.
        var options = Options.Create(new NormalizerOptions());
        var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

        var jsonPayload = """{"product":"Widget","qty":5}""";
        var xmlPayload = "<Product><name>Gadget</name><qty>10</qty></Product>";
        var csvPayload = "product,qty\nGizmo,3";

        var jsonResult = await normalizer.NormalizeAsync(jsonPayload, "application/json");
        var xmlResult = await normalizer.NormalizeAsync(xmlPayload, "application/xml");
        var csvResult = await normalizer.NormalizeAsync(csvPayload, "text/csv");

        // All results should be parsable JSON.
        Assert.DoesNotThrow(() => JsonDocument.Parse(jsonResult.Payload));
        Assert.DoesNotThrow(() => JsonDocument.Parse(xmlResult.Payload));
        Assert.DoesNotThrow(() => JsonDocument.Parse(csvResult.Payload));

        Assert.That(jsonResult.WasTransformed, Is.False);
        Assert.That(xmlResult.WasTransformed, Is.True);
        Assert.That(csvResult.WasTransformed, Is.True);
    }

    // ── Challenge 2: Nested XML Conversion ──────────────────────────────────

    [Test]
    public async Task Challenge2_NestedXml_ConvertedToNestedJson()
    {
        // XML with nested elements should produce nested JSON objects.
        var options = Options.Create(new NormalizerOptions());
        var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

        var xml = """
            <Order>
                <id>ORD-42</id>
                <customer>
                    <name>Alice</name>
                    <email>alice@example.com</email>
                </customer>
                <total>150.00</total>
            </Order>
            """;

        var result = await normalizer.NormalizeAsync(xml, "application/xml");

        Assert.That(result.DetectedFormat, Is.EqualTo("XML"));
        Assert.That(result.WasTransformed, Is.True);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.GetProperty("id").GetString(), Is.EqualTo("ORD-42"));
        Assert.That(
            doc.RootElement.GetProperty("customer").GetProperty("name").GetString(),
            Is.EqualTo("Alice"));
        Assert.That(
            doc.RootElement.GetProperty("customer").GetProperty("email").GetString(),
            Is.EqualTo("alice@example.com"));
    }

    // ── Challenge 3: CSV Without Headers ────────────────────────────────────

    [Test]
    public async Task Challenge3_CsvWithoutHeaders_ProducesArrayOfArrays()
    {
        // When CsvHasHeaders is false, each row should be a JSON array of values
        // rather than an object with named properties.
        var options = Options.Create(new NormalizerOptions { CsvHasHeaders = false });
        var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

        var csv = "Alice,30\nBob,25\nCharlie,35";

        var result = await normalizer.NormalizeAsync(csv, "text/csv");

        Assert.That(result.WasTransformed, Is.True);

        using var doc = JsonDocument.Parse(result.Payload);
        var array = doc.RootElement.GetProperty("Root");
        Assert.That(array.GetArrayLength(), Is.EqualTo(3));

        // Each row is an array of string values.
        Assert.That(array[0].GetArrayLength(), Is.EqualTo(2));
        Assert.That(array[0][0].GetString(), Is.EqualTo("Alice"));
        Assert.That(array[0][1].GetString(), Is.EqualTo("30"));
    }
}
