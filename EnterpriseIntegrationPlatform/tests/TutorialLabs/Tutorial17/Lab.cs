// ============================================================================
// Tutorial 17 – Normalizer (Lab)
// ============================================================================
// EIP Pattern: Normalizer.
// E2E: MessageNormalizer detecting JSON/XML/CSV and converting to canonical
// JSON. Publish normalized results via MockEndpoint.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial17;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("normalizer-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();


    // ── 1. Format Detection & Conversion ─────────────────────────────

    [Test]
    public async Task Normalize_Json_PassesThroughUnchanged()
    {
        var normalizer = CreateNormalizer();

        var result = await normalizer.NormalizeAsync(
            """{"name":"Alice","age":30}""", "application/json");

        Assert.That(result.DetectedFormat, Is.EqualTo("JSON"));
        Assert.That(result.WasTransformed, Is.False);
        Assert.That(result.Payload, Does.Contain("Alice"));
    }

    [Test]
    public async Task Normalize_Xml_ConvertsToJson()
    {
        var normalizer = CreateNormalizer();

        var xml = "<Root><name>Bob</name><age>25</age></Root>";
        var result = await normalizer.NormalizeAsync(xml, "application/xml");

        Assert.That(result.DetectedFormat, Is.EqualTo("XML"));
        Assert.That(result.WasTransformed, Is.True);
        Assert.That(result.Payload, Does.Contain("Bob"));
        Assert.That(result.Payload, Does.Contain("25"));
        Assert.That(result.OriginalContentType, Is.EqualTo("application/xml"));
    }

    [Test]
    public async Task Normalize_Csv_ConvertsToJsonArray()
    {
        var normalizer = CreateNormalizer();

        var csv = "name,age\nAlice,30\nBob,25";
        var result = await normalizer.NormalizeAsync(csv, "text/csv");

        Assert.That(result.DetectedFormat, Is.EqualTo("CSV"));
        Assert.That(result.WasTransformed, Is.True);
        Assert.That(result.Payload, Does.Contain("Alice"));
        Assert.That(result.Payload, Does.Contain("Bob"));
    }


    // ── 2. Strict vs Non-Strict Mode ─────────────────────────────────

    [Test]
    public async Task Normalize_StrictContentType_ThrowsForUnknown()
    {
        var normalizer = CreateNormalizer(strict: true);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => normalizer.NormalizeAsync("some data", "application/octet-stream"));
    }

    [Test]
    public async Task Normalize_NonStrict_DetectsJsonByPayload()
    {
        var normalizer = CreateNormalizer(strict: false);

        var result = await normalizer.NormalizeAsync(
            """{"key":"value"}""", "application/octet-stream");

        Assert.That(result.DetectedFormat, Is.EqualTo("JSON"));
        Assert.That(result.WasTransformed, Is.False);
    }


    // ── 3. End-to-End Integration ────────────────────────────────────

    [Test]
    public async Task Normalize_E2E_PublishNormalizedToMockEndpoint()
    {
        var normalizer = CreateNormalizer();

        var xml = "<Order><id>ORD-1</id><total>99</total></Order>";
        var result = await normalizer.NormalizeAsync(xml, "application/xml");

        var envelope = IntegrationEnvelope<string>.Create(
            result.Payload, "NormalizerService", "payload.normalized");
        await _output.PublishAsync(envelope, "normalized-topic", CancellationToken.None);

        _output.AssertReceivedOnTopic("normalized-topic", 1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Does.Contain("ORD-1"));
    }

    private static MessageNormalizer CreateNormalizer(bool strict = true) =>
        new(Options.Create(new NormalizerOptions { StrictContentType = strict }),
            NullLogger<MessageNormalizer>.Instance);
}
