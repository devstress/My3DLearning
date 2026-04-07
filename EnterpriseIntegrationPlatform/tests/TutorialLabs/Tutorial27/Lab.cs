// ============================================================================
// Tutorial 27 – Resequencer (Lab)
// ============================================================================
// EIP Pattern: Resequencer.
// Real Integrations: MessageResequencer buffers out-of-order messages, releases
// in sequence, then publishes results to NatsBrokerEndpoint (real NATS
// JetStream via Aspire).
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Resequencer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial27;

[TestFixture]
public sealed class Lab
{
    // ── 1. Ordering ──────────────────────────────────────────────────

    [Test]
    public async Task Accept_InOrder_ReleasesAllWhenComplete()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t27-inorder");
        var topic = AspireFixture.UniqueTopic("t27-ordered");
        var resequencer = CreateResequencer();
        var correlationId = Guid.NewGuid();

        var env1 = CreateEnvelope("p1", correlationId, sequenceNumber: 0, totalCount: 2);
        var result1 = resequencer.Accept(env1);
        Assert.That(result1, Is.Empty);

        var env2 = CreateEnvelope("p2", correlationId, sequenceNumber: 1, totalCount: 2);
        var result2 = resequencer.Accept(env2);
        Assert.That(result2, Has.Count.EqualTo(2));

        foreach (var env in result2)
            await nats.PublishAsync(env, topic);

        nats.AssertReceivedOnTopic(topic, 2);
    }

    [Test]
    public async Task Accept_OutOfOrder_ReleasesInCorrectSequence()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t27-outoforder");
        var topic = AspireFixture.UniqueTopic("t27-ordered");
        var resequencer = CreateResequencer();
        var correlationId = Guid.NewGuid();

        var env3 = CreateEnvelope("third", correlationId, sequenceNumber: 2, totalCount: 3);
        var env1 = CreateEnvelope("first", correlationId, sequenceNumber: 0, totalCount: 3);
        var env2 = CreateEnvelope("second", correlationId, sequenceNumber: 1, totalCount: 3);

        Assert.That(resequencer.Accept(env3), Is.Empty);
        Assert.That(resequencer.Accept(env1), Is.Empty);
        var released = resequencer.Accept(env2);

        Assert.That(released, Has.Count.EqualTo(3));
        Assert.That(released[0].Payload, Is.EqualTo("first"));
        Assert.That(released[1].Payload, Is.EqualTo("second"));
        Assert.That(released[2].Payload, Is.EqualTo("third"));

        foreach (var env in released)
            await nats.PublishAsync(env, topic);

        nats.AssertReceivedOnTopic(topic, 3);
    }


    // ── 2. Validation ────────────────────────────────────────────────

    [Test]
    public void Accept_DuplicateSequenceNumber_IsIgnored()
    {
        var resequencer = CreateResequencer();
        var correlationId = Guid.NewGuid();

        var env1 = CreateEnvelope("first", correlationId, sequenceNumber: 0, totalCount: 3);
        resequencer.Accept(env1);

        var dup = CreateEnvelope("dup-first", correlationId, sequenceNumber: 0, totalCount: 3);
        var result = resequencer.Accept(dup);

        Assert.That(result, Is.Empty);
        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(1));
    }

    [Test]
    public void Accept_MissingSequenceInfo_ThrowsArgumentException()
    {
        var resequencer = CreateResequencer();
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "evt");

        Assert.Throws<ArgumentException>(() => resequencer.Accept(envelope));
    }


    // ── 3. Timeout & State ───────────────────────────────────────────

    [Test]
    public async Task ReleaseOnTimeout_IncompleteSequence_ReleasesBuffered()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t27-timeout");
        var topic = AspireFixture.UniqueTopic("t27-timeout");
        var resequencer = CreateResequencer();
        var correlationId = Guid.NewGuid();

        resequencer.Accept(CreateEnvelope("a", correlationId, 0, 5));
        resequencer.Accept(CreateEnvelope("c", correlationId, 2, 5));

        var released = resequencer.ReleaseOnTimeout<string>(correlationId);
        Assert.That(released, Has.Count.EqualTo(2));
        Assert.That(released[0].SequenceNumber, Is.EqualTo(0));
        Assert.That(released[1].SequenceNumber, Is.EqualTo(2));

        foreach (var env in released)
            await nats.PublishAsync(env, topic);

        nats.AssertReceivedOnTopic(topic, 2);
        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(0));
    }

    [Test]
    public void ReleaseOnTimeout_UnknownCorrelation_ReturnsEmpty()
    {
        var resequencer = CreateResequencer();
        var released = resequencer.ReleaseOnTimeout<string>(Guid.NewGuid());
        Assert.That(released, Is.Empty);
    }

    [Test]
    public void ActiveSequenceCount_TracksBufferedSequences()
    {
        var resequencer = CreateResequencer();
        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(0));

        resequencer.Accept(CreateEnvelope("a", Guid.NewGuid(), 0, 3));
        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(1));

        resequencer.Accept(CreateEnvelope("b", Guid.NewGuid(), 0, 2));
        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(2));
    }

    private static MessageResequencer CreateResequencer() =>
        new(Options.Create(new ResequencerOptions()), NullLogger<MessageResequencer>.Instance);

    private static IntegrationEnvelope<string> CreateEnvelope(
        string payload, Guid correlationId, int sequenceNumber, int totalCount) =>
        IntegrationEnvelope<string>.Create(payload, "Svc", "evt") with
        {
            CorrelationId = correlationId,
            SequenceNumber = sequenceNumber,
            TotalCount = totalCount,
        };
}
