// ============================================================================
// Tutorial 17 – Normalizer (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Normalizer pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — XML with repeated elements produces JSON arrays
//   🟡 Intermediate — CSV with custom semicolon delimiter parses correctly
//   🔴 Advanced     — Multi-format batch normalization and publish via MockEndpoint
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial17;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — XML repeated elements produce JSON arrays ──────────
    //
    // SCENARIO: An inventory feed arrives as XML with repeated <item>
    //           elements. After normalization, the repeated siblings must
    //           appear as a JSON array containing all values.
    //
    // WHAT YOU PROVE: The normalizer correctly converts repeated XML
    //                 sibling elements into a JSON array structure.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_XmlRepeatedElements_ProducesJsonArrays()
    {
        var normalizer = new MessageNormalizer(
            Options.Create(new NormalizerOptions()),
            NullLogger<MessageNormalizer>.Instance);

        var xml = "<Root><item>A</item><item>B</item><item>C</item></Root>";
        var result = await normalizer.NormalizeAsync(xml, "application/xml");

        Assert.That(result.DetectedFormat, Is.EqualTo("XML"));
        Assert.That(result.WasTransformed, Is.True);
        // Repeated sibling elements become JSON arrays
        Assert.That(result.Payload, Does.Contain("["));
        Assert.That(result.Payload, Does.Contain("A"));
        Assert.That(result.Payload, Does.Contain("B"));
        Assert.That(result.Payload, Does.Contain("C"));
    }

    // ── 🟡 INTERMEDIATE — CSV with custom semicolon delimiter ───────────
    //
    // SCENARIO: A European partner sends product data as semicolon-delimited
    //           CSV. The normalizer must be configured with CsvDelimiter = ';'
    //           to correctly parse the fields.
    //
    // WHAT YOU PROVE: Custom delimiter configuration allows the normalizer
    //                 to parse non-standard CSV formats into canonical JSON.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_CsvCustomDelimiter_ParsesCorrectly()
    {
        var normalizer = new MessageNormalizer(
            Options.Create(new NormalizerOptions
            {
                CsvDelimiter = ';',
                CsvHasHeaders = true,
            }),
            NullLogger<MessageNormalizer>.Instance);

        var csv = "product;price\nWidget;9.99\nGadget;19.99";
        var result = await normalizer.NormalizeAsync(csv, "text/csv");

        Assert.That(result.DetectedFormat, Is.EqualTo("CSV"));
        Assert.That(result.WasTransformed, Is.True);
        Assert.That(result.Payload, Does.Contain("Widget"));
        Assert.That(result.Payload, Does.Contain("9.99"));
        Assert.That(result.Payload, Does.Contain("Gadget"));
    }

    // ── 🔴 ADVANCED — Multi-format batch normalize and publish ──────────
    //
    // SCENARIO: A gateway receives messages in JSON, XML, and CSV formats
    //           in a single batch. Each must be normalized to canonical JSON
    //           and published to a shared topic. All three formats must be
    //           correctly detected and the batch count verified.
    //
    // WHAT YOU PROVE: The normalizer handles a heterogeneous batch of
    //                 formats, normalizes each, and publishes end-to-end.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_MultiformatBatch_NormalizeAndPublish()
    {
        await using var output = new MockEndpoint("exam-normalizer");
        var normalizer = new MessageNormalizer(
            Options.Create(new NormalizerOptions()),
            NullLogger<MessageNormalizer>.Instance);

        var jsonResult = await normalizer.NormalizeAsync(
            """{"status":"ok"}""", "application/json");
        var xmlResult = await normalizer.NormalizeAsync(
            "<Root><status>ok</status></Root>", "application/xml");
        var csvResult = await normalizer.NormalizeAsync(
            "status\nok\ndone", "text/csv");

        foreach (var r in new[] { jsonResult, xmlResult, csvResult })
        {
            var envelope = IntegrationEnvelope<string>.Create(
                r.Payload, "NormSvc", "normalized");
            await output.PublishAsync(envelope, "canonical-json", CancellationToken.None);
        }

        output.AssertReceivedOnTopic("canonical-json", 3);
        Assert.That(jsonResult.DetectedFormat, Is.EqualTo("JSON"));
        Assert.That(xmlResult.DetectedFormat, Is.EqualTo("XML"));
        Assert.That(csvResult.DetectedFormat, Is.EqualTo("CSV"));
    }
}
