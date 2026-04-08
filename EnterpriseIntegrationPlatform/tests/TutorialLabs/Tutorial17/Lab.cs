// ============================================================================
// Tutorial 17 – Normalizer (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Normalizer pattern
//          auto-detects JSON, XML, and CSV payloads and converts them
//          to canonical JSON.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Normalize_Json_PassesThroughUnchanged           — JSON payload passes through without transformation
//   2. Normalize_Xml_ConvertsToJson                    — XML payload converts to canonical JSON
//   3. Normalize_Csv_ConvertsToJsonArray               — CSV payload converts to JSON array
//   4. Normalize_StrictContentType_ThrowsForUnknown    — strict mode throws for unknown content types
//   5. Normalize_NonStrict_DetectsJsonByPayload        — non-strict mode detects format by payload inspection
//   6. Normalize_E2E_PublishNormalizedToNatsEndpoint    — end-to-end normalize and publish via real NATS
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire)
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


    // ── 3. End-to-End Integration (Real NATS) ────────────────────────

    [Test]
    public async Task Normalize_E2E_PublishNormalizedToNatsEndpoint()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t17-e2e");
        var topic = AspireFixture.UniqueTopic("t17-normalized");

        var normalizer = CreateNormalizer();

        var xml = "<Order><id>ORD-1</id><total>99</total></Order>";
        var result = await normalizer.NormalizeAsync(xml, "application/xml");

        var envelope = IntegrationEnvelope<string>.Create(
            result.Payload, "NormalizerService", "payload.normalized");
        await nats.PublishAsync(envelope, topic, CancellationToken.None);

        nats.AssertReceivedOnTopic(topic, 1);
        var received = nats.GetReceived<string>();
        Assert.That(received.Payload, Does.Contain("ORD-1"));
    }

    private static MessageNormalizer CreateNormalizer(bool strict = true) =>
        new(Options.Create(new NormalizerOptions { StrictContentType = strict }),
            NullLogger<MessageNormalizer>.Instance);
}
