// ============================================================================
// Tutorial 17 – Normalizer (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — XML with repeated elements produces JSON arrays
//   🟡 Intermediate  — CSV with custom semicolon delimiter parses correctly
//   🔴 Advanced      — Multi-format batch normalization and publish via MockEndpoint
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
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
        // TODO: Create a MessageNormalizer with appropriate configuration
        dynamic normalizer = null!;

        var xml = "<Root><item>A</item><item>B</item><item>C</item></Root>";
        // TODO: var result = await normalizer.NormalizeAsync(...)
        dynamic result = null!;

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
        // TODO: Create a MessageNormalizer with appropriate configuration
        dynamic normalizer = null!;

        var csv = "product;price\nWidget;9.99\nGadget;19.99";
        // TODO: var result = await normalizer.NormalizeAsync(...)
        dynamic result = null!;

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
        // TODO: Create a MessageNormalizer with appropriate configuration
        dynamic normalizer = null!;

        // TODO: var jsonResult = await normalizer.NormalizeAsync(...)
        dynamic jsonResult = null!;
        // TODO: var xmlResult = await normalizer.NormalizeAsync(...)
        dynamic xmlResult = null!;
        // TODO: var csvResult = await normalizer.NormalizeAsync(...)
        dynamic csvResult = null!;

        foreach (var r in new[] { jsonResult, xmlResult, csvResult })
        {
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await output.PublishAsync(...)
        }

        output.AssertReceivedOnTopic("canonical-json", 3);
        Assert.That(jsonResult.DetectedFormat, Is.EqualTo("JSON"));
        Assert.That(xmlResult.DetectedFormat, Is.EqualTo("XML"));
        Assert.That(csvResult.DetectedFormat, Is.EqualTo("CSV"));
    }
}
#endif
