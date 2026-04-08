// ============================================================================
// Tutorial 26 – Message Replay (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Message Replay pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Filter by CorrelationId replays only matching messages
//   🟡 Intermediate — MaxMessages caps the number of replayed messages
//   🔴 Advanced     — Missing SourceTopic throws InvalidOperationException
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
using EnterpriseIntegrationPlatform.Processing.Replay;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial26;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Filter by CorrelationId ──────────────────────────
    //
    // SCENARIO: Three messages are stored — two share a target CorrelationId
    //           and one does not. Replay with a CorrelationId filter should
    //           replay only the two matching messages.
    //
    // WHAT YOU PROVE: The CorrelationId filter correctly selects matching
    //                 messages and the replay count is accurate.
    // ─────────────────────────────────────────────────────────────────────

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
    //
    // SCENARIO: Ten messages are stored but MaxMessages is set to 3. Replay
    //           should publish exactly 3 messages and stop.
    //
    // WHAT YOU PROVE: The MaxMessages option correctly caps the number of
    //                 messages replayed from the store.
    // ─────────────────────────────────────────────────────────────────────

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
    //
    // SCENARIO: A MessageReplayer is created with an empty SourceTopic.
    //           Attempting to replay must throw InvalidOperationException.
    //
    // WHAT YOU PROVE: The configuration guard prevents replay when the
    //                 source topic is not configured.
    // ─────────────────────────────────────────────────────────────────────

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
