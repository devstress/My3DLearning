// ============================================================================
// Tutorial 20 – Splitter (Lab)
// ============================================================================
// EIP Pattern: Splitter.
// E2E: MessageSplitter with FuncSplitStrategy + MockEndpoint to capture
// split messages, verify SequenceNumber, TotalCount, and CausationId.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Splitter;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial20;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("splitter-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();


    // ── 1. Split Output & Correlation ────────────────────────────────

    [Test]
    public async Task Split_ProducesCorrectItemCount()
    {
        var splitter = CreateStringSplitter(",");

        var source = IntegrationEnvelope<string>.Create(
            "A,B,C", "OrderService", "batch.created");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.ItemCount, Is.EqualTo(3));
        Assert.That(result.SplitEnvelopes, Has.Count.EqualTo(3));
        _output.AssertReceivedOnTopic("split-topic", 3);
    }

    [Test]
    public async Task Split_PreservesCorrelationId()
    {
        var splitter = CreateStringSplitter(",");

        var source = IntegrationEnvelope<string>.Create(
            "X,Y", "Svc", "batch");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.SplitEnvelopes[0].CorrelationId, Is.EqualTo(source.CorrelationId));
        Assert.That(result.SplitEnvelopes[1].CorrelationId, Is.EqualTo(source.CorrelationId));
    }

    [Test]
    public async Task Split_SetsCausationIdToSourceMessageId()
    {
        var splitter = CreateStringSplitter(",");

        var source = IntegrationEnvelope<string>.Create(
            "A,B", "Svc", "batch");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.SplitEnvelopes[0].CausationId, Is.EqualTo(source.MessageId));
        Assert.That(result.SplitEnvelopes[1].CausationId, Is.EqualTo(source.MessageId));
    }


    // ── 2. Sequence Metadata ─────────────────────────────────────────

    [Test]
    public async Task Split_SequenceNumbers_AreZeroBased()
    {
        var splitter = CreateStringSplitter(",");

        var source = IntegrationEnvelope<string>.Create(
            "A,B,C", "Svc", "batch");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.SplitEnvelopes[0].SequenceNumber, Is.EqualTo(0));
        Assert.That(result.SplitEnvelopes[1].SequenceNumber, Is.EqualTo(1));
        Assert.That(result.SplitEnvelopes[2].SequenceNumber, Is.EqualTo(2));
    }

    [Test]
    public async Task Split_TotalCount_MatchesItemCount()
    {
        var splitter = CreateStringSplitter(",");

        var source = IntegrationEnvelope<string>.Create(
            "A,B,C,D", "Svc", "batch");
        var result = await splitter.SplitAsync(source);

        foreach (var env in result.SplitEnvelopes)
            Assert.That(env.TotalCount, Is.EqualTo(4));

        Assert.That(result.ItemCount, Is.EqualTo(4));
    }


    // ── 3. Edge Cases ────────────────────────────────────────────────

    [Test]
    public async Task Split_EmptyResult_ReturnsZeroItems()
    {
        var strategy = new FuncSplitStrategy<string>(_ => Array.Empty<string>());
        var options = Options.Create(new SplitterOptions { TargetTopic = "split-topic" });
        var splitter = new MessageSplitter<string>(
            strategy, _output, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "empty", "Svc", "batch");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.ItemCount, Is.EqualTo(0));
        Assert.That(result.SplitEnvelopes, Is.Empty);
        _output.AssertNoneReceived();
    }

    [Test]
    public async Task Split_SourceMessageId_CapturedInResult()
    {
        var splitter = CreateStringSplitter(",");

        var source = IntegrationEnvelope<string>.Create(
            "A,B", "Svc", "batch");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.SourceMessageId, Is.EqualTo(source.MessageId));
        Assert.That(result.TargetTopic, Is.EqualTo("split-topic"));
    }

    private MessageSplitter<string> CreateStringSplitter(string delimiter)
    {
        var strategy = new FuncSplitStrategy<string>(
            composite => composite.Split(delimiter).ToList());
        var options = Options.Create(new SplitterOptions { TargetTopic = "split-topic" });
        return new MessageSplitter<string>(
            strategy, _output, options,
            NullLogger<MessageSplitter<string>>.Instance);
    }
}
