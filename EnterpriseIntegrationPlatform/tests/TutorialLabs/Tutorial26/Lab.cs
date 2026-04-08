// ============================================================================
// Tutorial 26 – Message Replay (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Message Store / Replay
//          pattern replays stored messages with filtering and deduplication.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Replay_SingleMessage_PublishesToTargetTopic        — single message replayed to target topic
//   2. Replay_MultipleMessages_ReplaysAll                — multiple stored messages all replayed
//   3. Replay_FilterByMessageType_OnlyMatchingReplayed   — MessageType filter replays matching only
//   4. Replay_EmptyStore_ReturnsZeroReplayed             — empty store returns zero counts
//   5. Replay_SkipAlreadyReplayed_SkipsTaggedMessages    — SkipAlreadyReplayed skips tagged messages
//   6. Replay_ResultTimestamps_ArePopulated              — StartedAt / CompletedAt populated
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire)
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Replay;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial26;

[TestFixture]
public sealed class Lab
{
    // ── 1. Basic Replay ──────────────────────────────────────────────

    [Test]
    public async Task Replay_SingleMessage_PublishesToTargetTopic()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t26-single");
        var topic = AspireFixture.UniqueTopic("t26-replay");
        var store = new InMemoryMessageReplayStore();
        var envelope = IntegrationEnvelope<string>.Create("order-1", "OrderService", "order.created");
        await store.StoreForReplayAsync(envelope, "source-topic", CancellationToken.None);

        var replayer = CreateReplayer(store, nats, topic);
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(1));
        Assert.That(result.FailedCount, Is.EqualTo(0));
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task Replay_MultipleMessages_ReplaysAll()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t26-multi");
        var topic = AspireFixture.UniqueTopic("t26-replay");
        var store = new InMemoryMessageReplayStore();
        for (var i = 0; i < 3; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"data-{i}", "Svc", "event.type");
            await store.StoreForReplayAsync(env, "source-topic", CancellationToken.None);
        }

        var replayer = CreateReplayer(store, nats, topic);
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(3));
        nats.AssertReceivedOnTopic(topic, 3);
    }


    // ── 2. Filtering ─────────────────────────────────────────────────

    [Test]
    public async Task Replay_FilterByMessageType_OnlyMatchingReplayed()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t26-filter");
        var topic = AspireFixture.UniqueTopic("t26-replay");
        var store = new InMemoryMessageReplayStore();
        await store.StoreForReplayAsync(
            IntegrationEnvelope<string>.Create("a", "Svc", "order.created"), "source-topic", CancellationToken.None);
        await store.StoreForReplayAsync(
            IntegrationEnvelope<string>.Create("b", "Svc", "payment.received"), "source-topic", CancellationToken.None);

        var replayer = CreateReplayer(store, nats, topic);
        var filter = new ReplayFilter { MessageType = "order.created" };
        var result = await replayer.ReplayAsync(filter, CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(1));
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task Replay_EmptyStore_ReturnsZeroReplayed()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t26-empty");
        var topic = AspireFixture.UniqueTopic("t26-replay");
        var store = new InMemoryMessageReplayStore();
        var replayer = CreateReplayer(store, nats, topic);
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(0));
        Assert.That(result.SkippedCount, Is.EqualTo(0));
        nats.AssertNoneReceived();
    }


    // ── 3. Behaviour & Metadata ──────────────────────────────────────

    [Test]
    public async Task Replay_SkipAlreadyReplayed_SkipsTaggedMessages()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t26-skip");
        var topic = AspireFixture.UniqueTopic("t26-replay");
        var store = new InMemoryMessageReplayStore();
        var env = IntegrationEnvelope<string>.Create("data", "Svc", "event") with
        {
            Metadata = new Dictionary<string, string> { [MessageHeaders.ReplayId] = Guid.NewGuid().ToString() },
        };
        await store.StoreForReplayAsync(env, "source-topic", CancellationToken.None);

        var opts = new ReplayOptions
        {
            SourceTopic = "source-topic",
            TargetTopic = topic,
            MaxMessages = 100,
            SkipAlreadyReplayed = true,
        };
        var replayer = new MessageReplayer(store, nats, Options.Create(opts), NullLogger<MessageReplayer>.Instance);
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.SkippedCount, Is.EqualTo(1));
        Assert.That(result.ReplayedCount, Is.EqualTo(0));
        nats.AssertNoneReceived();
    }

    [Test]
    public async Task Replay_ResultTimestamps_ArePopulated()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t26-timestamps");
        var topic = AspireFixture.UniqueTopic("t26-replay");
        var store = new InMemoryMessageReplayStore();
        await store.StoreForReplayAsync(
            IntegrationEnvelope<string>.Create("d", "Svc", "evt"), "source-topic", CancellationToken.None);

        var replayer = CreateReplayer(store, nats, topic);
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.StartedAt, Is.LessThanOrEqualTo(result.CompletedAt));
        Assert.That(result.CompletedAt, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }

    private static MessageReplayer CreateReplayer(
        InMemoryMessageReplayStore store, NatsBrokerEndpoint nats, string topic)
    {
        var opts = Options.Create(new ReplayOptions
        {
            SourceTopic = "source-topic",
            TargetTopic = topic,
            MaxMessages = 100,
        });
        return new MessageReplayer(store, nats, opts, NullLogger<MessageReplayer>.Instance);
    }
}
