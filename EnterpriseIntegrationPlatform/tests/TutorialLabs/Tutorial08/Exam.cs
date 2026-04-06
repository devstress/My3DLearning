// ============================================================================
// Tutorial 08 – Activities and Pipeline (Exam)
// ============================================================================
// Coding challenges: build a metadata-enrichment activity and create a
// pipeline orchestrator that chains three activities together.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial08;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Metadata Enrichment Activity ───────────────────────────

    [Test]
    public void Challenge1_EnrichMetadata_AddsExpectedKeys()
    {
        // Build a custom "activity" that enriches an envelope's metadata
        // with processing context: timestamp, processor name, and a trace ID.
        var envelope = IntegrationEnvelope<string>.Create(
            "raw-data", "IngestService", "data.raw");

        // Simulate an enrichment activity — adds metadata via `with` expression.
        var enriched = EnrichMetadata(envelope, "MetadataEnricher", Guid.NewGuid().ToString());

        // Verify the metadata was added without losing existing data.
        Assert.That(enriched.Metadata.ContainsKey("processed-by"), Is.True);
        Assert.That(enriched.Metadata["processed-by"], Is.EqualTo("MetadataEnricher"));
        Assert.That(enriched.Metadata.ContainsKey("trace-id"), Is.True);
        Assert.That(enriched.Metadata.ContainsKey("processed-at"), Is.True);

        // Original envelope identity is preserved.
        Assert.That(enriched.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(enriched.Payload, Is.EqualTo(envelope.Payload));
    }

    [Test]
    public void Challenge1_EnrichMetadata_PreservesExistingMetadata()
    {
        // Metadata enrichment must NOT overwrite existing keys.
        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Service", "data.event") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["tenant-id"] = "T-100",
                ["source-region"] = "eu-west",
            },
        };

        var enriched = EnrichMetadata(envelope, "Enricher", "trace-abc");

        Assert.That(enriched.Metadata["tenant-id"], Is.EqualTo("T-100"));
        Assert.That(enriched.Metadata["source-region"], Is.EqualTo("eu-west"));
        Assert.That(enriched.Metadata["processed-by"], Is.EqualTo("Enricher"));
        Assert.That(enriched.Metadata["trace-id"], Is.EqualTo("trace-abc"));
    }

    /// <summary>
    /// Metadata enrichment activity — adds processing context to an envelope.
    /// </summary>
    private static IntegrationEnvelope<T> EnrichMetadata<T>(
        IntegrationEnvelope<T> envelope, string processorName, string traceId)
    {
        var newMetadata = new Dictionary<string, string>(envelope.Metadata)
        {
            ["processed-by"] = processorName,
            ["trace-id"] = traceId,
            ["processed-at"] = DateTimeOffset.UtcNow.ToString("O"),
        };

        return envelope with { Metadata = newMetadata };
    }

    // ── Challenge 2: Pipeline Orchestrator with 3 Activities ────────────────

    [Test]
    public async Task Challenge2_PipelineOrchestrator_ChainsThreeActivities()
    {
        // Build a pipeline orchestrator that chains:
        //   Activity 1: Validate
        //   Activity 2: Enrich metadata
        //   Activity 3: Publish to destination
        //
        // If validation fails, the pipeline stops and routes to a DLQ.
        var validationService = Substitute.For<IMessageValidationService>();
        var producer = Substitute.For<IMessageBrokerProducer>();

        const string messageType = "shipment.dispatched";
        const string payloadJson = "{\"shipmentId\": \"SH-42\", \"carrier\": \"FastShip\"}";

        validationService.ValidateAsync(messageType, payloadJson)
            .Returns(MessageValidationResult.Success);

        var envelope = IntegrationEnvelope<string>.Create(
            payloadJson, "ShipmentService", messageType);

        // --- Pipeline Execution ---

        // Activity 1: Validate.
        var validation = await validationService.ValidateAsync(messageType, payloadJson);
        Assert.That(validation.IsValid, Is.True);

        // Activity 2: Enrich metadata.
        envelope = EnrichMetadata(envelope, "PipelineOrchestrator", Guid.NewGuid().ToString());
        Assert.That(envelope.Metadata.ContainsKey("processed-by"), Is.True);

        // Activity 3: Publish to destination.
        await producer.PublishAsync(envelope, "shipments.processed");

        // Verify the full chain.
        await validationService.Received(1).ValidateAsync(messageType, payloadJson);
        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e =>
                e.Metadata.ContainsKey("processed-by") &&
                e.Metadata["processed-by"] == "PipelineOrchestrator"),
            Arg.Is("shipments.processed"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Challenge2_PipelineOrchestrator_ValidationFails_RoutesToDlq()
    {
        // When validation fails, the pipeline should route to a DLQ topic
        // and NOT publish to the normal destination.
        var validationService = Substitute.For<IMessageValidationService>();
        var producer = Substitute.For<IMessageBrokerProducer>();

        const string messageType = "shipment.dispatched";
        const string badPayload = "not-json";

        validationService.ValidateAsync(messageType, badPayload)
            .Returns(MessageValidationResult.Failure("Invalid JSON payload"));

        var envelope = IntegrationEnvelope<string>.Create(
            badPayload, "ShipmentService", messageType);

        // Activity 1: Validate — fails.
        var validation = await validationService.ValidateAsync(messageType, badPayload);
        Assert.That(validation.IsValid, Is.False);

        // Pipeline stops — route to DLQ instead.
        if (!validation.IsValid)
        {
            await producer.PublishAsync(envelope, "shipments.dlq");
        }

        // Verify: DLQ got the message, normal topic did not.
        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("shipments.dlq"),
            Arg.Any<CancellationToken>());

        await producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("shipments.processed"),
            Arg.Any<CancellationToken>());
    }
}
