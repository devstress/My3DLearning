// ============================================================================
// Tutorial 20 – Splitter (Exam)
// ============================================================================
// E2E challenges: split with custom message type override, metadata
// preservation across splits, and large batch split with MockEndpoint
// verification.
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
    [Test]
    public async Task Challenge1_TargetMessageTypeOverride_AppliedToAll()
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

    [Test]
    public async Task Challenge2_MetadataPreserved_AcrossSplitEnvelopes()
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

    [Test]
    public async Task Challenge3_LargeBatch_AllItemsPublished()
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
