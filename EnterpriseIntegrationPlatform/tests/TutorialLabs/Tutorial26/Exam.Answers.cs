// ============================================================================
// Tutorial 26 – Message Replay (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Filter by CorrelationId replays only matching messages
//   🟡 Intermediate — MaxMessages caps the number of replayed messages
//   🔴 Advanced     — Missing SourceTopic throws InvalidOperationException
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Replay;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial26;

[TestFixture]
public sealed class ExamAnswers
{
    // ── 🟢 STARTER — Filter by CorrelationId ──────────────────────────

    [Test]
    public async Task Starter_FilterByCorrelationId_OnlyMatchingReplayed()
    {
        await using var output = new MockEndpoint("replay-corr");
        var store = new InMemoryMessageReplayStore();
        var targetCorrelation = Guid.NewGuid();

        var env1 = IntegrationEnvelope<string>.Create("a", "Svc", "evt") with { CorrelationId = targetCorrelation };
        var env2 = IntegrationEnvelope<string>.Create("b", "Svc", "evt");
        var env3 = IntegrationEnvelope<string>.Create("c", "Svc", "evt") with { CorrelationId = targetCorrelation };
        await store.StoreForReplayAsync(env1, "src", CancellationToken.None);
        await store.StoreForReplayAsync(env2, "src", CancellationToken.None);
        await store.StoreForReplayAsync(env3, "src", CancellationToken.None);

        var opts = Options.Create(new ReplayOptions { SourceTopic = "src", TargetTopic = "tgt", MaxMessages = 100 });
        var replayer = new MessageReplayer(store, output, opts, NullLogger<MessageReplayer>.Instance);
        var result = await replayer.ReplayAsync(new ReplayFilter { CorrelationId = targetCorrelation }, CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(2));
        output.AssertReceivedOnTopic("tgt", 2);
    }

    // ── 🟡 INTERMEDIATE — MaxMessages caps replay count ────────────────

    [Test]
    public async Task Intermediate_MaxMessages_CapsReplayCount()
    {
        await using var output = new MockEndpoint("replay-max");
        var store = new InMemoryMessageReplayStore();
        for (var i = 0; i < 10; i++)
            await store.StoreForReplayAsync(
                IntegrationEnvelope<string>.Create($"d{i}", "Svc", "evt"), "src", CancellationToken.None);

        var opts = Options.Create(new ReplayOptions { SourceTopic = "src", TargetTopic = "tgt", MaxMessages = 3 });
        var replayer = new MessageReplayer(store, output, opts, NullLogger<MessageReplayer>.Instance);
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(3));
        output.AssertReceivedCount(3);
    }

    // ── 🔴 ADVANCED — Missing SourceTopic throws ──────────────────────

    [Test]
    public async Task Advanced_MissingSourceTopic_ThrowsInvalidOperation()
    {
        await using var output = new MockEndpoint("replay-err");
        var store = new InMemoryMessageReplayStore();
        var opts = Options.Create(new ReplayOptions { SourceTopic = "", TargetTopic = "tgt" });
        var replayer = new MessageReplayer(store, output, opts, NullLogger<MessageReplayer>.Instance);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None));
    }
}
