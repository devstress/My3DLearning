// ============================================================================
// Tutorial 04 – Integration Envelope (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — FaultEnvelope lifecycle with retry exhaustion
//   🟡 Intermediate — Multi-hop causation chain through PointToPointChannel
//   🔴 Advanced     — Split-sequence reassembly with full metadata verification
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial04;

#pragma warning disable CS0219 // Variable is assigned but its value is never used — intentional for fill-in-the-blank TODOs

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — FaultEnvelope Lifecycle with Retry Exhaustion ─────
    //
    // SCENARIO: A message fails processing 3 times due to database timeouts.
    // After retry exhaustion, a FaultEnvelope is created capturing the
    // original message identity, the final exception details, and the retry
    // count — ready for dead-letter queue routing and later replay.
    //
    // WHAT YOU PROVE: You can create a FaultEnvelope that preserves
    // correlation from the original message and captures exception details.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public void Starter_FaultEnvelope_RetryExhaustion()
    {
        // Simulate retry exhaustion: message fails 3 times, then generates
        // a FaultEnvelope capturing the final exception and all retry attempts.
        // Verify every field is correctly populated for dead-letter routing.
        // TODO: Create an IntegrationEnvelope<string> with payload "{\"orderId\":\"ORD-999\"}", source "IngestService", type "order.created", Priority = High, Intent = Command
        IntegrationEnvelope<string> original = null!; // ← replace with IntegrationEnvelope<string>.Create(...)

        // TODO: Create a TimeoutException with message "Database connection timeout after 30s"
        TimeoutException exception = null!; // ← replace with new TimeoutException(...)

        // TODO: Create a FaultEnvelope from 'original', faulted by "PersistenceService", reason "Retry exhaustion after 3 attempts", retryCount 3, passing the exception
        FaultEnvelope fault = null!; // ← replace with FaultEnvelope.Create(...)

        // Identity preserved from original
        Assert.That(fault.OriginalMessageId, Is.EqualTo(original.MessageId));
        Assert.That(fault.CorrelationId, Is.EqualTo(original.CorrelationId));
        Assert.That(fault.OriginalMessageType, Is.EqualTo("order.created"));

        // Fault-specific fields
        Assert.That(fault.FaultId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(fault.FaultedBy, Is.EqualTo("PersistenceService"));
        Assert.That(fault.RetryCount, Is.EqualTo(3));
        Assert.That(fault.FaultedAt.Date, Is.EqualTo(DateTimeOffset.UtcNow.Date));

        // Exception captured for diagnostics
        Assert.That(fault.ErrorDetails, Does.Contain("TimeoutException"));
        Assert.That(fault.ErrorDetails, Does.Contain("Database connection timeout"));

        // Each FaultEnvelope gets its own unique FaultId
        // TODO: Create a second FaultEnvelope from 'original', faulted by "PersistenceService", reason "Second failure", retryCount 4
        FaultEnvelope fault2 = null!; // ← replace with FaultEnvelope.Create(...)
        Assert.That(fault2.FaultId, Is.Not.EqualTo(fault.FaultId));
    }

    // ── 🟡 INTERMEDIATE — Multi-Hop Causation Through Channel ─────────
    //
    // SCENARIO: A web application submits a "PlaceOrder" command (hop 1).
    // The order service processes it and emits an "OrderPlaced" event
    // (hop 2). The billing service reacts and generates an invoice document
    // (hop 3). Each message flows through a PointToPointChannel with the
    // correct causation chain and message intent.
    //
    // WHAT YOU PROVE: You can build a three-hop causation chain where
    // Command → Event → Document flows through channels with correct
    // lineage and intent at each hop.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_CausationChain_ThreeHopsThroughChannel()
    {
        // Build a three-hop causation chain where each message is delivered
        // through a PointToPointChannel: Command → Event → Document.
        // Verify the full causation lineage and shared CorrelationId.
        var output = new MockEndpoint("chain");
        var channel = new PointToPointChannel(
            output, output, NullLogger<PointToPointChannel>.Instance);

        // Hop 1: Command originates the business transaction
        // TODO: Create an IntegrationEnvelope<string> with payload "PlaceOrder", source "WebApp", type "order.place", Intent = Command
        IntegrationEnvelope<string> command = null!; // ← replace with IntegrationEnvelope<string>.Create(...)
        await channel.SendAsync(command, "commands", CancellationToken.None);

        // Hop 2: Event reports the command was processed
        // TODO: Create an IntegrationEnvelope<string> with payload "OrderPlaced", source "OrderService", type "order.placed", correlationId from command, causationId from command.MessageId, Intent = Event
        IntegrationEnvelope<string> evt = null!; // ← replace with IntegrationEnvelope<string>.Create(...)
        await channel.SendAsync(evt, "events", CancellationToken.None);

        // Hop 3: Document generated as a side effect
        // TODO: Create an IntegrationEnvelope<string> with payload "InvoiceGenerated", source "BillingService", type "invoice.generated", correlationId from command, causationId from evt.MessageId, Intent = Document
        IntegrationEnvelope<string> doc = null!; // ← replace with IntegrationEnvelope<string>.Create(...)
        await channel.SendAsync(doc, "documents", CancellationToken.None);

        output.AssertReceivedCount(3);
        var rCmd = output.GetReceived<string>(0);
        var rEvt = output.GetReceived<string>(1);
        var rDoc = output.GetReceived<string>(2);

        // Causation chain: Command → Event → Document
        Assert.That(rCmd.CausationId, Is.Null);
        Assert.That(rEvt.CausationId, Is.EqualTo(rCmd.MessageId));
        Assert.That(rDoc.CausationId, Is.EqualTo(rEvt.MessageId));

        // All share the same business transaction correlation
        Assert.That(rEvt.CorrelationId, Is.EqualTo(rCmd.CorrelationId));
        Assert.That(rDoc.CorrelationId, Is.EqualTo(rCmd.CorrelationId));

        // Each has the correct intent
        Assert.That(rCmd.Intent, Is.EqualTo(MessageIntent.Command));
        Assert.That(rEvt.Intent, Is.EqualTo(MessageIntent.Event));
        Assert.That(rDoc.Intent, Is.EqualTo(MessageIntent.Document));

        // Routed to correct topics
        output.AssertReceivedOnTopic("commands", 1);
        output.AssertReceivedOnTopic("events", 1);
        output.AssertReceivedOnTopic("documents", 1);

        await output.DisposeAsync();
    }

    // ── 🔴 ADVANCED — Split Sequence with Full Metadata ────────────────
    //
    // SCENARIO: A Splitter decomposes a large dataset into 5 chunks, each
    // carrying SequenceNumber, TotalCount, shared CorrelationId, High
    // priority, and custom metadata headers. All chunks must survive
    // delivery through a PointToPointChannel with every field intact.
    //
    // WHAT YOU PROVE: You can produce a complete split sequence with full
    // metadata and verify reassembly-ready delivery through a channel.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_SplitSequence_AllPartsWithMetadataPreserved()
    {
        // A Splitter produces a sequence of messages: each carries
        // SequenceNumber, TotalCount, shared CorrelationId, and custom
        // metadata. All must survive delivery through a real channel.
        var output = new MockEndpoint("split");
        var channel = new PointToPointChannel(
            output, output, NullLogger<PointToPointChannel>.Instance);

        var correlationId = Guid.NewGuid();
        const int total = 5;

        for (var i = 0; i < total; i++)
        {
            // TODO: Create an IntegrationEnvelope<string> with payload $"chunk-{i}", source "Splitter",
            //   type "data.chunk", correlationId from correlationId variable, and set:
            //   SequenceNumber = i, TotalCount = total, Priority = High,
            //   Metadata with MessageHeaders.SequenceNumber and MessageHeaders.TotalCount
            IntegrationEnvelope<string> part = null!; // ← replace with IntegrationEnvelope<string>.Create(...)

            // TODO: Send the part through the channel on topic "chunks"
            await Task.CompletedTask; // ← replace with channel.SendAsync(...)
        }

        output.AssertReceivedCount(total);
        var all = output.GetAllReceived<string>("chunks");

        for (var i = 0; i < total; i++)
        {
            Assert.That(all[i].SequenceNumber, Is.EqualTo(i));
            Assert.That(all[i].TotalCount, Is.EqualTo(total));
            Assert.That(all[i].Payload, Is.EqualTo($"chunk-{i}"));
            Assert.That(all[i].CorrelationId, Is.EqualTo(correlationId));
            Assert.That(all[i].Priority, Is.EqualTo(MessagePriority.High));
            Assert.That(all[i].Metadata[MessageHeaders.SequenceNumber], Is.EqualTo(i.ToString()));
        }

        await output.DisposeAsync();
    }
}
#endif
