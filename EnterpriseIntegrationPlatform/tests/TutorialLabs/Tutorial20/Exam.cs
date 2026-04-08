// ============================================================================
// Tutorial 20 – Splitter (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Target message type override applied to all split envelopes
//   🟡 Intermediate  — Metadata, priority, and schema version preserved across splits
//   🔴 Advanced      — Large batch of 50 items all published with correct sequence numbers
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Splitter;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
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

        // TODO: Create a FuncSplitStrategy with appropriate configuration
        dynamic strategy = null!;
        // TODO: var options = Options.Create(...)
        dynamic options = null!;
        // TODO: Create a MessageSplitter with appropriate configuration
        dynamic splitter = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic source = null!;
        // TODO: var result = await splitter.SplitAsync(...)
        dynamic result = null!;

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

        // TODO: Create a FuncSplitStrategy with appropriate configuration
        dynamic strategy = null!;
        // TODO: var options = Options.Create(...)
        dynamic options = null!;
        // TODO: Create a MessageSplitter with appropriate configuration
        dynamic splitter = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic source = null!;

        // TODO: var result = await splitter.SplitAsync(...)
        dynamic result = null!;

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
        // TODO: Create a FuncSplitStrategy with appropriate configuration
        dynamic strategy = null!;
        // TODO: var options = Options.Create(...)
        dynamic options = null!;
        // TODO: Create a MessageSplitter with appropriate configuration
        dynamic splitter = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic source = null!;
        // TODO: var result = await splitter.SplitAsync(...)
        dynamic result = null!;

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
#endif
