// ============================================================================
// Tutorial 08 – Activities Pipeline (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply pipeline patterns in realistic scenarios —
//          metadata enrichment, DLQ routing on failure, and full multi-stage
//          verification with audit logging.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Enrich envelope metadata and verify preservation through pipeline
//   🟡 Intermediate — Route invalid messages to DLQ while skipping normal output
//   🔴 Advanced     — Full Persist → Validate → Publish pipeline with audit logging
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint / MockPersistenceActivityService / MockMessageLoggingService
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial08;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Metadata Enrichment Through Pipeline ─────────────────
    //
    // SCENARIO: An order processing pipeline validates incoming orders, then
    // enriches them with processing metadata (processed-by, region) before
    // publishing to the enriched queue.
    //
    // WHAT YOU PROVE: You can validate, enrich metadata, and publish through
    // a Point-to-Point channel while preserving all added metadata fields.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Starter_EnrichAndPublish_MetadataPreserved()
    {
        var validator = new DefaultMessageValidationService();
        await using var output = new MockEndpoint("enriched");

        var envelope = IntegrationEnvelope<string>.Create(
            "{\"orderId\":\"ORD-42\"}", "OrderService", "order.created");

        var result = await validator.ValidateAsync(
            envelope.MessageType, envelope.Payload);
        Assert.That(result.IsValid, Is.True);

        var enriched = envelope with
        {
            Metadata = new Dictionary<string, string>(envelope.Metadata)
            {
                ["processed-by"] = "Pipeline",
                ["region"] = "us-east",
            },
        };

        var channel = new PointToPointChannel(
            output, output, NullLogger<PointToPointChannel>.Instance);
        await channel.SendAsync(enriched, "enriched-queue", CancellationToken.None);

        output.AssertReceivedCount(1);
        var received = output.GetReceived<string>();
        Assert.That(received.Metadata["processed-by"], Is.EqualTo("Pipeline"));
        Assert.That(received.Metadata["region"], Is.EqualTo("us-east"));
    }

    // ── 🟡 INTERMEDIATE — DLQ Routing on Validation Failure ────────────────
    //
    // SCENARIO: A legacy system sends non-JSON payloads that fail validation.
    // The pipeline must route these to a dead-letter queue via the Invalid
    // Message Channel while ensuring no messages reach the normal output.
    //
    // WHAT YOU PROVE: You can implement conditional routing — valid messages
    // to output, invalid messages to DLQ — with proper topic verification.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Intermediate_ValidationFailure_RoutesDlqAndSkipsOutput()
    {
        var validator = new DefaultMessageValidationService();
        await using var goodOutput = new MockEndpoint("good");
        await using var dlqOutput = new MockEndpoint("dlq");

        var envelope = IntegrationEnvelope<string>.Create(
            "not-json", "LegacySystem", "legacy.event");

        var result = await validator.ValidateAsync(
            envelope.MessageType, envelope.Payload);

        if (result.IsValid)
        {
            await goodOutput.PublishAsync(envelope, "good-topic");
        }
        else
        {
            var invalidOpts = Options.Create(new InvalidMessageChannelOptions
                { InvalidMessageTopic = "dlq-topic", Source = "Pipeline" });
            var invalidChannel = new InvalidMessageChannel(
                dlqOutput, invalidOpts, NullLogger<InvalidMessageChannel>.Instance);
            await invalidChannel.RouteInvalidAsync(
                envelope, result.Reason!, CancellationToken.None);
        }

        goodOutput.AssertNoneReceived();
        dlqOutput.AssertReceivedCount(1);
        dlqOutput.AssertReceivedOnTopic("dlq-topic", 1);
    }

    // ── 🔴 ADVANCED — Full Multi-Stage Pipeline with Audit ─────────────────
    //
    // SCENARIO: An exam processing service runs a complete four-stage pipeline:
    // Persist the message, log the persistence, validate the payload, log the
    // validation, publish to the final topic, and log the publication.
    //
    // WHAT YOU PROVE: You can orchestrate a full Persist → Validate → Publish
    // pipeline with audit logging at every stage and verify all stages executed.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Advanced_MultiStage_PersistValidatePublishVerify()
    {
        var persistence = new MockPersistenceActivityService();
        var logging = new MockMessageLoggingService();
        var validator = new DefaultMessageValidationService();
        await using var output = new MockEndpoint("final");

        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(), CorrelationId: Guid.NewGuid(),
            CausationId: null, Timestamp: DateTimeOffset.UtcNow,
            Source: "ExamService", MessageType: "exam.event", SchemaVersion: "1.0",
            Priority: 2, PayloadJson: "{\"exam\":true}", MetadataJson: null,
            AckSubject: "ack.exam", NackSubject: "nack.exam");

        await persistence.SaveMessageAsync(input);
        await logging.LogAsync(input.MessageId, input.MessageType, "Persisted");

        var validation = await validator.ValidateAsync(
            input.MessageType, input.PayloadJson);
        Assert.That(validation.IsValid, Is.True);
        await logging.LogAsync(input.MessageId, input.MessageType, "Validated");

        var envelope = IntegrationEnvelope<string>.Create(
            input.PayloadJson, input.Source, input.MessageType);
        await output.PublishAsync(envelope, "final-topic");
        await logging.LogAsync(input.MessageId, input.MessageType, "Published");

        output.AssertReceivedCount(1);
        persistence.AssertSaveCount(1);
        logging.AssertLogged(input.MessageId, "Persisted");
        logging.AssertLogged(input.MessageId, "Validated");
        logging.AssertLogged(input.MessageId, "Published");
    }
}
