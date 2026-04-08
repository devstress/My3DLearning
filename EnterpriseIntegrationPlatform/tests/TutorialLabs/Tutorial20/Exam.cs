// ============================================================================
// Tutorial 20 – Splitter (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Splitter pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Target message type override applied to all split envelopes
//   🟡 Intermediate — Metadata, priority, and schema version preserved across splits
//   🔴 Advanced     — Large batch of 50 items all published with correct sequence numbers
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
using EnterpriseIntegrationPlatform.Processing.Splitter;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial20;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Target message type override ──────────────────────
    //
    // SCENARIO: An order batch "X,Y,Z" arrives from OriginalSvc. The splitter
    //           is configured with TargetMessageType and TargetSource overrides.
    //           Every split envelope must carry the overridden values.
    //
    // WHAT YOU PROVE: SplitterOptions.TargetMessageType and TargetSource are
    //                 applied to every split envelope and all items are published.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_TargetMessageTypeOverride_AppliedToAll()
    {
        await using var output = new MockEndpoint("exam-splitter-1");

        var strategy = new FuncSplitStrategy<string>(
            composite => composite.Split(',').ToList());
        var options = Options.Create(new SplitterOptions
        {
            TargetTopic = "items-topic",
            TargetMessageType = "item.split",
            TargetSource = "SplitterService",
        });
        var splitter = new MessageSplitter<string>(
            strategy, output, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "X,Y,Z", "OriginalSvc", "batch.original");
        var result = await splitter.SplitAsync(source);

        foreach (var env in result.SplitEnvelopes)
        {
            Assert.That(env.MessageType, Is.EqualTo("item.split"));
            Assert.That(env.Source, Is.EqualTo("SplitterService"));
        }

        output.AssertReceivedOnTopic("items-topic", 3);
    }

    // ── 🟡 INTERMEDIATE — Metadata preserved across split envelopes ────
    //
    // SCENARIO: A batch message "A|B" carries region/priority metadata,
    //           MessagePriority.High, and SchemaVersion "2.0". After splitting,
    //           every child envelope must retain all of these properties.
    //
    // WHAT YOU PROVE: The splitter faithfully copies metadata, priority, and
    //                 schema version from the source envelope to every split.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_MetadataPreserved_AcrossSplitEnvelopes()
    {
        await using var output = new MockEndpoint("exam-splitter-2");

        var strategy = new FuncSplitStrategy<string>(
            composite => composite.Split('|').ToList());
        var options = Options.Create(new SplitterOptions { TargetTopic = "meta-topic" });
        var splitter = new MessageSplitter<string>(
            strategy, output, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "A|B", "Svc", "batch") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["region"] = "us-east",
                ["priority"] = "high",
            },
            Priority = MessagePriority.High,
            SchemaVersion = "2.0",
        };

        var result = await splitter.SplitAsync(source);

        foreach (var env in result.SplitEnvelopes)
        {
            Assert.That(env.Metadata["region"], Is.EqualTo("us-east"));
            Assert.That(env.Metadata["priority"], Is.EqualTo("high"));
            Assert.That(env.Priority, Is.EqualTo(MessagePriority.High));
            Assert.That(env.SchemaVersion, Is.EqualTo("2.0"));
        }

        output.AssertReceivedOnTopic("meta-topic", 2);
    }

    // ── 🔴 ADVANCED — Large batch split with full verification ─────────
    //
    // SCENARIO: Fifty items are joined into a single comma-separated payload.
    //           The splitter must produce exactly 50 envelopes, publish all to
    //           "bulk-topic", and assign SequenceNumbers 0..49 with TotalCount 50
    //           on every envelope.
    //
    // WHAT YOU PROVE: The splitter handles large batches end-to-end with
    //                 correct sequence numbering and total count metadata.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_LargeBatch_AllItemsPublished()
    {
        await using var output = new MockEndpoint("exam-splitter-3");

        var items = Enumerable.Range(1, 50).Select(i => $"item-{i}").ToList();
        var strategy = new FuncSplitStrategy<string>(
            composite => composite.Split(',').ToList());
        var options = Options.Create(new SplitterOptions { TargetTopic = "bulk-topic" });
        var splitter = new MessageSplitter<string>(
            strategy, output, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            string.Join(",", items), "BulkSvc", "batch.large");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.ItemCount, Is.EqualTo(50));
        output.AssertReceivedOnTopic("bulk-topic", 50);

        // Verify sequence numbers span 0..49
        Assert.That(result.SplitEnvelopes.First().SequenceNumber, Is.EqualTo(0));
        Assert.That(result.SplitEnvelopes.Last().SequenceNumber, Is.EqualTo(49));

        // Verify TotalCount on every envelope
        foreach (var env in result.SplitEnvelopes)
            Assert.That(env.TotalCount, Is.EqualTo(50));
    }
}
