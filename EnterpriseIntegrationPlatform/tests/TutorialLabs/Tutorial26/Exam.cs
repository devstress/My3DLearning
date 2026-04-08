// ============================================================================
// Tutorial 26 – Message Replay (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Filter by CorrelationId replays only matching messages
//   🟡 Intermediate  — MaxMessages caps the number of replayed messages
//   🔴 Advanced      — Missing SourceTopic throws InvalidOperationException
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Replay;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
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
        // TODO: Create a InMemoryMessageReplayStore with appropriate configuration
        dynamic store = null!;
        var targetCorrelation = Guid.NewGuid();

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic env1 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic env2 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic env3 = null!;
        await store.StoreForReplayAsync(env1, "src", CancellationToken.None);
        await store.StoreForReplayAsync(env2, "src", CancellationToken.None);
        await store.StoreForReplayAsync(env3, "src", CancellationToken.None);

        // TODO: var opts = Options.Create(...)
        dynamic opts = null!;
        // TODO: Create a MessageReplayer with appropriate configuration
        dynamic replayer = null!;
        // TODO: var result = await replayer.ReplayAsync(...)
        dynamic result = null!;

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
        // TODO: Create a InMemoryMessageReplayStore with appropriate configuration
        dynamic store = null!;
        for (var i = 0; i < 10; i++)
            // TODO: await store.StoreForReplayAsync(...)

        // TODO: var opts = Options.Create(...)
        dynamic opts = null!;
        // TODO: Create a MessageReplayer with appropriate configuration
        dynamic replayer = null!;
        // TODO: var result = await replayer.ReplayAsync(...)
        dynamic result = null!;

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
        // TODO: Create a InMemoryMessageReplayStore with appropriate configuration
        dynamic store = null!;
        // TODO: var opts = Options.Create(...)
        dynamic opts = null!;
        // TODO: Create a MessageReplayer with appropriate configuration
        dynamic replayer = null!;

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None));
    }
}
#endif
