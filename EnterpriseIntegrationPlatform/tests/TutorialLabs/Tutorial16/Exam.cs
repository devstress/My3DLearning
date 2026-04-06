// ============================================================================
// Tutorial 16 – Transform Pipeline (Exam)
// ============================================================================
// Coding challenges: build a JSON→XML→JSON round-trip pipeline, compose a
// regex-replace pipeline, and exercise concrete transform steps end-to-end.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial16;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: JSON → XML Round-Trip ──────────────────────────────────

    [Test]
    public async Task Challenge1_JsonToXmlStep_ProducesValidXml()
    {
        // Use the real JsonToXmlStep to convert a simple JSON object to XML.
        var step = new JsonToXmlStep("Order");
        var options = Options.Create(new TransformOptions());
        var pipeline = new TransformPipeline(
            new ITransformStep[] { step }, options,
            NullLogger<TransformPipeline>.Instance);

        var json = """{"orderId":"ORD-1","amount":"250"}""";

        var result = await pipeline.ExecuteAsync(json, "application/json");

        Assert.That(result.ContentType, Is.EqualTo("application/xml"));
        Assert.That(result.Payload, Does.Contain("<Order>"));
        Assert.That(result.Payload, Does.Contain("<orderId>ORD-1</orderId>"));
        Assert.That(result.Payload, Does.Contain("<amount>250</amount>"));
        Assert.That(result.StepsApplied, Is.EqualTo(1));
        Assert.That(result.Metadata.ContainsKey("Step.JsonToXml.Applied"), Is.True);
    }

    // ── Challenge 2: Regex Replace Pipeline ─────────────────────────────────

    [Test]
    public async Task Challenge2_RegexReplacePipeline_SanitisesPayload()
    {
        // Build a two-step pipeline that first masks credit card numbers, then
        // redacts email addresses from a plain-text payload.
        var maskCards = new RegexReplaceStep(
            @"\b\d{4}-\d{4}-\d{4}-\d{4}\b", "****-****-****-****");
        var redactEmails = new RegexReplaceStep(
            @"[\w.+-]+@[\w-]+\.[\w.]+", "[REDACTED]");

        var options = Options.Create(new TransformOptions());
        var pipeline = new TransformPipeline(
            new ITransformStep[] { maskCards, redactEmails }, options,
            NullLogger<TransformPipeline>.Instance);

        var input = "Card: 1234-5678-9012-3456, Email: alice@example.com";

        var result = await pipeline.ExecuteAsync(input, "text/plain");

        Assert.That(result.Payload, Does.Contain("****-****-****-****"));
        Assert.That(result.Payload, Does.Contain("[REDACTED]"));
        Assert.That(result.Payload, Does.Not.Contain("1234-5678-9012-3456"));
        Assert.That(result.Payload, Does.Not.Contain("alice@example.com"));
        Assert.That(result.StepsApplied, Is.EqualTo(2));
    }

    // ── Challenge 3: XmlToJson Step End-to-End ──────────────────────────────

    [Test]
    public async Task Challenge3_XmlToJsonStep_ConvertsXmlToJson()
    {
        // Use the real XmlToJsonStep to convert an XML document to JSON.
        var step = new XmlToJsonStep();
        var options = Options.Create(new TransformOptions());
        var pipeline = new TransformPipeline(
            new ITransformStep[] { step }, options,
            NullLogger<TransformPipeline>.Instance);

        var xml = "<Root><name>Alice</name><age>30</age></Root>";

        var result = await pipeline.ExecuteAsync(xml, "application/xml");

        Assert.That(result.ContentType, Is.EqualTo("application/json"));
        Assert.That(result.StepsApplied, Is.EqualTo(1));

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.GetProperty("name").GetString(), Is.EqualTo("Alice"));
        Assert.That(doc.RootElement.GetProperty("age").GetString(), Is.EqualTo("30"));
    }
}
