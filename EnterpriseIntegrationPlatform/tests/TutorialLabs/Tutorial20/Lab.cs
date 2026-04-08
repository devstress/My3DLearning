// ============================================================================
// Tutorial 20 – Splitter (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Splitter pattern breaks
//          composite messages into individual items using IMessageSplitter<T>
//          with pluggable ISplitStrategy<T>.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Split_ProducesCorrectItemCount           — split a composite string and verify item count
//   2. Split_PreservesCorrelationId             — split envelopes inherit the source CorrelationId
//   3. Split_SetsCausationIdToSourceMessageId   — each split envelope's CausationId equals source MessageId
//   4. Split_SequenceNumbers_AreZeroBased       — split envelopes have zero-based SequenceNumber values
//   5. Split_TotalCount_MatchesItemCount        — every split envelope's TotalCount matches item count
//   6. Split_EmptyResult_ReturnsZeroItems       — empty strategy result produces zero items and no publishes
//   7. Split_SourceMessageId_CapturedInResult   — SplitResult captures the original source MessageId
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire)
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
    // ── 1. Split Output & Correlation (Real NATS) ────────────────────

    [Test]
    public async Task Split_ProducesCorrectItemCount()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t20-count");
        var topic = AspireFixture.UniqueTopic("t20-split");
        var splitter = CreateStringSplitter(nats, topic, ",");

        var source = IntegrationEnvelope<string>.Create(
            "A,B,C", "OrderService", "batch.created");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.ItemCount, Is.EqualTo(3));
        Assert.That(result.SplitEnvelopes, Has.Count.EqualTo(3));
        nats.AssertReceivedOnTopic(topic, 3);
    }

    [Test]
    public async Task Split_PreservesCorrelationId()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t20-corr");
        var topic = AspireFixture.UniqueTopic("t20-split");
        var splitter = CreateStringSplitter(nats, topic, ",");

        var source = IntegrationEnvelope<string>.Create(
            "X,Y", "Svc", "batch");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.SplitEnvelopes[0].CorrelationId, Is.EqualTo(source.CorrelationId));
        Assert.That(result.SplitEnvelopes[1].CorrelationId, Is.EqualTo(source.CorrelationId));
    }

    [Test]
    public async Task Split_SetsCausationIdToSourceMessageId()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t20-caus");
        var topic = AspireFixture.UniqueTopic("t20-split");
        var splitter = CreateStringSplitter(nats, topic, ",");

        var source = IntegrationEnvelope<string>.Create(
            "A,B", "Svc", "batch");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.SplitEnvelopes[0].CausationId, Is.EqualTo(source.MessageId));
        Assert.That(result.SplitEnvelopes[1].CausationId, Is.EqualTo(source.MessageId));
    }


    // ── 2. Sequence Metadata (Real NATS) ─────────────────────────────

    [Test]
    public async Task Split_SequenceNumbers_AreZeroBased()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t20-seq");
        var topic = AspireFixture.UniqueTopic("t20-split");
        var splitter = CreateStringSplitter(nats, topic, ",");

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
        await using var nats = AspireFixture.CreateNatsEndpoint("t20-total");
        var topic = AspireFixture.UniqueTopic("t20-split");
        var splitter = CreateStringSplitter(nats, topic, ",");

        var source = IntegrationEnvelope<string>.Create(
            "A,B,C,D", "Svc", "batch");
        var result = await splitter.SplitAsync(source);

        foreach (var env in result.SplitEnvelopes)
            Assert.That(env.TotalCount, Is.EqualTo(4));

        Assert.That(result.ItemCount, Is.EqualTo(4));
    }


    // ── 3. Edge Cases (Real NATS) ────────────────────────────────────

    [Test]
    public async Task Split_EmptyResult_ReturnsZeroItems()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t20-empty");
        var topic = AspireFixture.UniqueTopic("t20-split");

        var strategy = new FuncSplitStrategy<string>(_ => Array.Empty<string>());
        var options = Options.Create(new SplitterOptions { TargetTopic = topic });
        var splitter = new MessageSplitter<string>(
            strategy, nats, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "empty", "Svc", "batch");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.ItemCount, Is.EqualTo(0));
        Assert.That(result.SplitEnvelopes, Is.Empty);
        nats.AssertNoneReceived();
    }

    [Test]
    public async Task Split_SourceMessageId_CapturedInResult()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t20-srcid");
        var topic = AspireFixture.UniqueTopic("t20-split");
        var splitter = CreateStringSplitter(nats, topic, ",");

        var source = IntegrationEnvelope<string>.Create(
            "A,B", "Svc", "batch");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.SourceMessageId, Is.EqualTo(source.MessageId));
        Assert.That(result.TargetTopic, Is.EqualTo(topic));
    }

    private static MessageSplitter<string> CreateStringSplitter(
        NatsBrokerEndpoint nats, string topic, string delimiter)
    {
        var strategy = new FuncSplitStrategy<string>(
            composite => composite.Split(delimiter).ToList());
        var options = Options.Create(new SplitterOptions { TargetTopic = topic });
        return new MessageSplitter<string>(
            strategy, nats, options,
            NullLogger<MessageSplitter<string>>.Instance);
    }
}
