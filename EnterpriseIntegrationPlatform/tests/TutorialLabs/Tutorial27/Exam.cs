// ============================================================================
// Tutorial 27 – Resequencer (Exam)
// ============================================================================
// E2E challenges: large out-of-order batch, interleaved sequences, timeout
// partial release.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Resequencer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial27;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_LargeOutOfOrderBatch_ReleasedInSequence()
    {
        await using var output = new MockEndpoint("reseq-batch");
        var resequencer = new MessageResequencer(
            Options.Create(new ResequencerOptions()), NullLogger<MessageResequencer>.Instance);
        var correlationId = Guid.NewGuid();
        const int total = 10;

        var indices = Enumerable.Range(0, total).Reverse().ToList();
        IReadOnlyList<IntegrationEnvelope<string>> released = [];
        foreach (var i in indices)
        {
            var env = IntegrationEnvelope<string>.Create($"msg-{i}", "Svc", "evt") with
            {
                CorrelationId = correlationId, SequenceNumber = i, TotalCount = total,
            };
            released = resequencer.Accept(env);
        }

        Assert.That(released, Has.Count.EqualTo(total));
        for (var i = 0; i < total; i++)
            Assert.That(released[i].SequenceNumber, Is.EqualTo(i));

        foreach (var env in released)
            await output.PublishAsync(env, "ordered");

        output.AssertReceivedOnTopic("ordered", total);
    }

    [Test]
    public async Task Challenge2_InterleavedSequences_EachReleasedIndependently()
    {
        await using var output = new MockEndpoint("reseq-interleave");
        var resequencer = new MessageResequencer(
            Options.Create(new ResequencerOptions()), NullLogger<MessageResequencer>.Instance);

        var corrA = Guid.NewGuid();
        var corrB = Guid.NewGuid();

        resequencer.Accept(CreateEnvelope("A1", corrA, 1, 2));
        resequencer.Accept(CreateEnvelope("B0", corrB, 0, 2));

        var releasedA = resequencer.Accept(CreateEnvelope("A0", corrA, 0, 2));
        Assert.That(releasedA, Has.Count.EqualTo(2));
        Assert.That(releasedA[0].Payload, Is.EqualTo("A0"));

        var releasedB = resequencer.Accept(CreateEnvelope("B1", corrB, 1, 2));
        Assert.That(releasedB, Has.Count.EqualTo(2));
        Assert.That(releasedB[0].Payload, Is.EqualTo("B0"));

        foreach (var env in releasedA.Concat(releasedB))
            await output.PublishAsync(env, "interleaved");

        output.AssertReceivedOnTopic("interleaved", 4);
    }

    [Test]
    public async Task Challenge3_TimeoutPartialRelease_ThenCompleteNewSequence()
    {
        await using var output = new MockEndpoint("reseq-timeout");
        var resequencer = new MessageResequencer(
            Options.Create(new ResequencerOptions()), NullLogger<MessageResequencer>.Instance);

        var corrOld = Guid.NewGuid();
        resequencer.Accept(CreateEnvelope("old-0", corrOld, 0, 5));
        resequencer.Accept(CreateEnvelope("old-3", corrOld, 3, 5));

        var partial = resequencer.ReleaseOnTimeout<string>(corrOld);
        Assert.That(partial, Has.Count.EqualTo(2));

        foreach (var env in partial)
            await output.PublishAsync(env, "partial");

        var corrNew = Guid.NewGuid();
        resequencer.Accept(CreateEnvelope("new-1", corrNew, 1, 2));
        var complete = resequencer.Accept(CreateEnvelope("new-0", corrNew, 0, 2));
        Assert.That(complete, Has.Count.EqualTo(2));

        foreach (var env in complete)
            await output.PublishAsync(env, "complete");

        output.AssertReceivedOnTopic("partial", 2);
        output.AssertReceivedOnTopic("complete", 2);
        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(0));
    }

    private static IntegrationEnvelope<string> CreateEnvelope(
        string payload, Guid correlationId, int sequenceNumber, int totalCount) =>
        IntegrationEnvelope<string>.Create(payload, "Svc", "evt") with
        {
            CorrelationId = correlationId,
            SequenceNumber = sequenceNumber,
            TotalCount = totalCount,
        };
}
