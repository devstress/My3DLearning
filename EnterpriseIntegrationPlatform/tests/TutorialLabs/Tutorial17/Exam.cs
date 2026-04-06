// ============================================================================
// Tutorial 17 – Normalizer (Exam)
// ============================================================================
// E2E challenges: XML with repeated elements (arrays), CSV with custom
// delimiter, and multi-format batch normalization via MockEndpoint.
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
    [Test]
    public async Task Challenge1_XmlRepeatedElements_ProducesJsonArrays()
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

    [Test]
    public async Task Challenge2_CsvCustomDelimiter_ParsesCorrectly()
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

    [Test]
    public async Task Challenge3_MultiformatBatch_NormalizeAndPublish()
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
