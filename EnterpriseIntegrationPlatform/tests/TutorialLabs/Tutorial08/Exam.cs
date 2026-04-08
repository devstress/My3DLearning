// ============================================================================
// Tutorial 08 – Activities Pipeline (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Enrich envelope metadata and verify preservation through pipeline
//   🟡 Intermediate — Route invalid messages to DLQ while skipping normal output
//   🔴 Advanced     — Full Persist → Validate → Publish pipeline with audit logging
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
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

        // TODO: Create an IntegrationEnvelope<string> with payload "{\"orderId\":\"ORD-42\"}", source "OrderService", type "order.created"
        IntegrationEnvelope<string> envelope = null!; // ← replace with IntegrationEnvelope<string>.Create(...)

        var result = await validator.ValidateAsync(
            envelope.MessageType, envelope.Payload);
        Assert.That(result.IsValid, Is.True);

        // TODO: Create an enriched copy of envelope using `with` expression,
        //       adding Metadata keys "processed-by" = "Pipeline" and "region" = "us-east"
        IntegrationEnvelope<string> enriched = null!; // ← replace with envelope with { Metadata = ... }

        // TODO: Create a PointToPointChannel with output, output, and NullLogger
        PointToPointChannel channel = null!; // ← replace with new PointToPointChannel(...)
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
            // TODO: Create InvalidMessageChannelOptions with InvalidMessageTopic = "dlq-topic", Source = "Pipeline"
            // TODO: Create an InvalidMessageChannel with dlqOutput, the options, and NullLogger
            // TODO: Call RouteInvalidAsync on invalidChannel with envelope, result.Reason!, CancellationToken.None
            _ = dlqOutput; // placeholder — replace this block with InvalidMessageChannel creation + RouteInvalidAsync call
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

        // TODO: Create an IntegrationPipelineInput with:
        //   MessageId: Guid.NewGuid(), CorrelationId: Guid.NewGuid(), CausationId: null,
        //   Timestamp: DateTimeOffset.UtcNow, Source: "ExamService", MessageType: "exam.event",
        //   SchemaVersion: "1.0", Priority: 2, PayloadJson: "{\"exam\":true}", MetadataJson: null,
        //   AckSubject: "ack.exam", NackSubject: "nack.exam"
        IntegrationPipelineInput input = null!; // ← replace with new IntegrationPipelineInput(...)

        // TODO: Persist — call persistence.SaveMessageAsync(input)
        // TODO: Log — call logging.LogAsync(input.MessageId, input.MessageType, "Persisted")

        // TODO: Validate — call validator.ValidateAsync(input.MessageType, input.PayloadJson)
        MessageValidationResult validation = new(true, null); // ← replace with await validator.ValidateAsync(...)
        Assert.That(validation.IsValid, Is.True);
        // TODO: Log — call logging.LogAsync(input.MessageId, input.MessageType, "Validated")

        // TODO: Publish — create envelope from input, publish to "final-topic"
        // TODO: Log — call logging.LogAsync(input.MessageId, input.MessageType, "Published")

        output.AssertReceivedCount(1);
        persistence.AssertSaveCount(1);
        logging.AssertLogged(input.MessageId, "Persisted");
        logging.AssertLogged(input.MessageId, "Validated");
        logging.AssertLogged(input.MessageId, "Published");
    }
}
#endif
