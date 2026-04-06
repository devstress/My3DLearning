// ============================================================================
// Tutorial 27 – Resequencer (Lab)
// ============================================================================
// This lab exercises the MessageResequencer, which buffers out-of-order
// messages and releases them in sequence-number order once complete.
// You will verify ordering, buffering, timeout release, and duplicate handling.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Resequencer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial27;

[TestFixture]
public sealed class Lab
{
    private MessageResequencer CreateResequencer(int maxConcurrent = 10_000)
    {
        var options = Options.Create(new ResequencerOptions
        {
            MaxConcurrentSequences = maxConcurrent,
        });
        return new MessageResequencer(options, NullLogger<MessageResequencer>.Instance);
    }

    private static IntegrationEnvelope<string> MakeSequenced(
        Guid correlationId, int seqNum, int totalCount) =>
        new()
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
            Source = "Svc",
            MessageType = "type",
            Payload = $"msg-{seqNum}",
            SequenceNumber = seqNum,
            TotalCount = totalCount,
        };

    // ── In-Order Delivery Releases Immediately ───────────────────────────────

    [Test]
    public void Accept_CompleteSequenceInOrder_ReleasesAllMessages()
    {
        var resequencer = CreateResequencer();
        var correlationId = Guid.NewGuid();

        var r1 = resequencer.Accept(MakeSequenced(correlationId, 0, 3));
        var r2 = resequencer.Accept(MakeSequenced(correlationId, 1, 3));
        var r3 = resequencer.Accept(MakeSequenced(correlationId, 2, 3));

        // Only the last accept should release all 3
        Assert.That(r1, Is.Empty);
        Assert.That(r2, Is.Empty);
        Assert.That(r3, Has.Count.EqualTo(3));
        Assert.That(r3[0].Payload, Is.EqualTo("msg-0"));
        Assert.That(r3[1].Payload, Is.EqualTo("msg-1"));
        Assert.That(r3[2].Payload, Is.EqualTo("msg-2"));
    }

    // ── Out-Of-Order Delivery Reorders Correctly ─────────────────────────────

    [Test]
    public void Accept_OutOfOrder_ReleasesInCorrectOrder()
    {
        var resequencer = CreateResequencer();
        var correlationId = Guid.NewGuid();

        var r1 = resequencer.Accept(MakeSequenced(correlationId, 2, 3));
        var r2 = resequencer.Accept(MakeSequenced(correlationId, 0, 3));
        var r3 = resequencer.Accept(MakeSequenced(correlationId, 1, 3));

        Assert.That(r1, Is.Empty);
        Assert.That(r2, Is.Empty);
        Assert.That(r3, Has.Count.EqualTo(3));
        Assert.That(r3[0].Payload, Is.EqualTo("msg-0"));
        Assert.That(r3[1].Payload, Is.EqualTo("msg-1"));
        Assert.That(r3[2].Payload, Is.EqualTo("msg-2"));
    }

    // ── Incomplete Sequence Stays Buffered ───────────────────────────────────

    [Test]
    public void Accept_IncompleteSequence_BuffersAndReturnsEmpty()
    {
        var resequencer = CreateResequencer();
        var correlationId = Guid.NewGuid();

        var result = resequencer.Accept(MakeSequenced(correlationId, 1, 3));

        Assert.That(result, Is.Empty);
        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(1));
    }

    // ── Duplicate Sequence Number Is Ignored ─────────────────────────────────

    [Test]
    public void Accept_DuplicateSequenceNumber_IsIgnored()
    {
        var resequencer = CreateResequencer();
        var correlationId = Guid.NewGuid();

        resequencer.Accept(MakeSequenced(correlationId, 0, 2));
        var dup = resequencer.Accept(MakeSequenced(correlationId, 0, 2));

        Assert.That(dup, Is.Empty);
        // Still waiting for seq 1
        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(1));
    }

    // ── ReleaseOnTimeout Returns Buffered Messages In Order ──────────────────

    [Test]
    public void ReleaseOnTimeout_IncompleteSequence_ReturnsBufferedInOrder()
    {
        var resequencer = CreateResequencer();
        var correlationId = Guid.NewGuid();

        resequencer.Accept(MakeSequenced(correlationId, 2, 5));
        resequencer.Accept(MakeSequenced(correlationId, 0, 5));

        var released = resequencer.ReleaseOnTimeout<string>(correlationId);

        Assert.That(released, Has.Count.EqualTo(2));
        Assert.That(released[0].Payload, Is.EqualTo("msg-0"));
        Assert.That(released[1].Payload, Is.EqualTo("msg-2"));
        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(0));
    }

    // ── Missing Sequence Info Throws ─────────────────────────────────────────

    [Test]
    public void Accept_NoSequenceInfo_ThrowsArgumentException()
    {
        var resequencer = CreateResequencer();
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "type");

        Assert.Throws<ArgumentException>(() => resequencer.Accept(envelope));
    }

    // ── Single Message Sequence Releases Immediately ─────────────────────────

    [Test]
    public void Accept_SingleMessageSequence_ReleasesImmediately()
    {
        var resequencer = CreateResequencer();
        var correlationId = Guid.NewGuid();

        var result = resequencer.Accept(MakeSequenced(correlationId, 0, 1));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Payload, Is.EqualTo("msg-0"));
        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(0));
    }
}
