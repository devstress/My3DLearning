// ============================================================================
// Tutorial 27 – Resequencer (Exam)
// ============================================================================
// Coding challenges: multiple independent sequences, ResequencerOptions
// defaults, and ReleaseOnTimeout for unknown correlation ID.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Resequencer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial27;

[TestFixture]
public sealed class Exam
{
    private static MessageResequencer CreateResequencer() =>
        new(Options.Create(new ResequencerOptions()), NullLogger<MessageResequencer>.Instance);

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

    // ── Challenge 1: Multiple Independent Sequences ─────────────────────────

    [Test]
    public void Challenge1_TwoIndependentSequences_EachReleasedSeparately()
    {
        var resequencer = CreateResequencer();
        var seqA = Guid.NewGuid();
        var seqB = Guid.NewGuid();

        // Interleave messages from two sequences
        resequencer.Accept(MakeSequenced(seqA, 1, 2));
        resequencer.Accept(MakeSequenced(seqB, 0, 2));
        var releaseA = resequencer.Accept(MakeSequenced(seqA, 0, 2));
        var releaseB = resequencer.Accept(MakeSequenced(seqB, 1, 2));

        Assert.That(releaseA, Has.Count.EqualTo(2));
        Assert.That(releaseA[0].Payload, Is.EqualTo("msg-0"));
        Assert.That(releaseA[1].Payload, Is.EqualTo("msg-1"));

        Assert.That(releaseB, Has.Count.EqualTo(2));
        Assert.That(releaseB[0].Payload, Is.EqualTo("msg-0"));
        Assert.That(releaseB[1].Payload, Is.EqualTo("msg-1"));

        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(0));
    }

    // ── Challenge 2: ResequencerOptions Default Values ──────────────────────

    [Test]
    public void Challenge2_ResequencerOptions_DefaultValues()
    {
        var opts = new ResequencerOptions();

        Assert.That(opts.ReleaseTimeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(opts.MaxConcurrentSequences, Is.EqualTo(10_000));
    }

    // ── Challenge 3: ReleaseOnTimeout For Unknown CorrelationId ─────────────

    [Test]
    public void Challenge3_ReleaseOnTimeout_UnknownCorrelationId_ReturnsEmpty()
    {
        var resequencer = CreateResequencer();

        var result = resequencer.ReleaseOnTimeout<string>(Guid.NewGuid());

        Assert.That(result, Is.Empty);
    }
}
