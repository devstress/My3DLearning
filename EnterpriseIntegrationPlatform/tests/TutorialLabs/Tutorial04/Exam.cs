// ============================================================================
// Tutorial 04 – Integration Envelope (Exam)
// ============================================================================
// EIP Patterns: Envelope Wrapper, Fault Message, Message History
// End-to-End: FaultEnvelope lifecycle with exception capture and retry
// exhaustion, multi-hop causation chain through PointToPointChannel, and
// split-sequence reassembly with full metadata verification.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace TutorialLabs.Tutorial04;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: FaultEnvelope Lifecycle ─────────────────────────────

    [Test]
    public void Challenge1_FaultEnvelope_RetryExhaustion()
    {
        // Simulate retry exhaustion: message fails 3 times, then generates
        // a FaultEnvelope capturing the final exception and all retry attempts.
        // Verify every field is correctly populated for dead-letter routing.
        var original = IntegrationEnvelope<string>.Create(
            "{\"orderId\":\"ORD-999\"}", "IngestService", "order.created") with
        {
            Priority = MessagePriority.High,
            Intent = MessageIntent.Command,
        };

        var exception = new TimeoutException("Database connection timeout after 30s");
        var fault = FaultEnvelope.Create(
            original, "PersistenceService",
            "Retry exhaustion after 3 attempts", 3, exception);

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
        var fault2 = FaultEnvelope.Create(
            original, "PersistenceService", "Second failure", 4);
        Assert.That(fault2.FaultId, Is.Not.EqualTo(fault.FaultId));
    }

    // ── Challenge 2: Multi-Hop Causation Through Channel ────────────────

    [Test]
    public async Task Challenge2_CausationChain_ThreeHopsThroughChannel()
    {
        // Build a three-hop causation chain where each message is delivered
        // through a PointToPointChannel: Command → Event → Document.
        // Verify the full causation lineage and shared CorrelationId.
        var output = new MockEndpoint("chain");
        var channel = new PointToPointChannel(
            output, output, NullLogger<PointToPointChannel>.Instance);

        // Hop 1: Command originates the business transaction
        var command = IntegrationEnvelope<string>.Create(
            "PlaceOrder", "WebApp", "order.place") with
        {
            Intent = MessageIntent.Command,
        };
        await channel.SendAsync(command, "commands", CancellationToken.None);

        // Hop 2: Event reports the command was processed
        var evt = IntegrationEnvelope<string>.Create(
            "OrderPlaced", "OrderService", "order.placed",
            correlationId: command.CorrelationId,
            causationId: command.MessageId) with
        {
            Intent = MessageIntent.Event,
        };
        await channel.SendAsync(evt, "events", CancellationToken.None);

        // Hop 3: Document generated as a side effect
        var doc = IntegrationEnvelope<string>.Create(
            "InvoiceGenerated", "BillingService", "invoice.generated",
            correlationId: command.CorrelationId,
            causationId: evt.MessageId) with
        {
            Intent = MessageIntent.Document,
        };
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

    // ── Challenge 3: Split Sequence with Full Metadata ──────────────────

    [Test]
    public async Task Challenge3_SplitSequence_AllPartsWithMetadataPreserved()
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
            var part = IntegrationEnvelope<string>.Create(
                $"chunk-{i}", "Splitter", "data.chunk",
                correlationId: correlationId) with
            {
                SequenceNumber = i,
                TotalCount = total,
                Priority = MessagePriority.High,
                Metadata = new Dictionary<string, string>
                {
                    [MessageHeaders.SequenceNumber] = i.ToString(),
                    [MessageHeaders.TotalCount] = total.ToString(),
                },
            };
            await channel.SendAsync(part, "chunks", CancellationToken.None);
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
