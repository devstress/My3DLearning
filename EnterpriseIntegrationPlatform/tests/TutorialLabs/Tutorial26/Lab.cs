// ============================================================================
// Tutorial 26 – Message Replay (Lab)
// ============================================================================
// This lab exercises the MessageReplayer, ReplayFilter, ReplayResult,
// ReplayOptions, and the InMemoryMessageReplayStore.
// You will verify replay filtering, deduplication, and result reporting.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Replay;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial26;

[TestFixture]
public sealed class Lab
{
    // ── Replay Returns Correct Counts ────────────────────────────────────────

    [Test]
    public async Task Replay_AllMessagesReplayed_CountsAreCorrect()
    {
        var store = new InMemoryMessageReplayStore();
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new ReplayOptions
        {
            SourceTopic = "orders",
            TargetTopic = "orders-replay",
            MaxMessages = 100,
        });

        var replayer = new MessageReplayer(
            store, producer, options, NullLogger<MessageReplayer>.Instance);

        var env1 = IntegrationEnvelope<string>.Create("p1", "Svc", "order.created");
        var env2 = IntegrationEnvelope<string>.Create("p2", "Svc", "order.created");
        await store.StoreForReplayAsync(env1, "orders", CancellationToken.None);
        await store.StoreForReplayAsync(env2, "orders", CancellationToken.None);

        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(2));
        Assert.That(result.SkippedCount, Is.EqualTo(0));
        Assert.That(result.FailedCount, Is.EqualTo(0));
    }

    // ── Replay Publishes To Target Topic ─────────────────────────────────────

    [Test]
    public async Task Replay_PublishesToConfiguredTargetTopic()
    {
        var store = new InMemoryMessageReplayStore();
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new ReplayOptions
        {
            SourceTopic = "events",
            TargetTopic = "events-replay",
            MaxMessages = 10,
        });

        var replayer = new MessageReplayer(
            store, producer, options, NullLogger<MessageReplayer>.Instance);

        var env = IntegrationEnvelope<string>.Create("data", "Svc", "event.fired");
        await store.StoreForReplayAsync(env, "events", CancellationToken.None);

        await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<object>>(),
            "events-replay",
            Arg.Any<CancellationToken>());
    }

    // ── ReplayFilter By MessageType ──────────────────────────────────────────

    [Test]
    public async Task Replay_FilterByMessageType_OnlyMatchingMessagesReplayed()
    {
        var store = new InMemoryMessageReplayStore();
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new ReplayOptions
        {
            SourceTopic = "topic",
            TargetTopic = "topic-replay",
            MaxMessages = 100,
        });

        var replayer = new MessageReplayer(
            store, producer, options, NullLogger<MessageReplayer>.Instance);

        var match = IntegrationEnvelope<string>.Create("m", "Svc", "order.created");
        var noMatch = IntegrationEnvelope<string>.Create("n", "Svc", "invoice.created");
        await store.StoreForReplayAsync(match, "topic", CancellationToken.None);
        await store.StoreForReplayAsync(noMatch, "topic", CancellationToken.None);

        var filter = new ReplayFilter { MessageType = "order.created" };
        var result = await replayer.ReplayAsync(filter, CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(1));
    }

    // ── SkipAlreadyReplayed Deduplication ────────────────────────────────────

    [Test]
    public async Task Replay_SkipAlreadyReplayed_SkipsMessagesWithReplayIdHeader()
    {
        var store = new InMemoryMessageReplayStore();
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new ReplayOptions
        {
            SourceTopic = "src",
            TargetTopic = "tgt",
            MaxMessages = 100,
            SkipAlreadyReplayed = true,
        });

        var replayer = new MessageReplayer(
            store, producer, options, NullLogger<MessageReplayer>.Instance);

        var alreadyReplayed = new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "Svc",
            MessageType = "type",
            Payload = "data",
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.ReplayId] = Guid.NewGuid().ToString(),
            },
        };
        var fresh = IntegrationEnvelope<string>.Create("fresh", "Svc", "type");

        await store.StoreForReplayAsync(alreadyReplayed, "src", CancellationToken.None);
        await store.StoreForReplayAsync(fresh, "src", CancellationToken.None);

        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(1));
        Assert.That(result.SkippedCount, Is.EqualTo(1));
    }

    // ── Empty SourceTopic Throws ─────────────────────────────────────────────

    [Test]
    public void Replay_EmptySourceTopic_ThrowsInvalidOperationException()
    {
        var store = new InMemoryMessageReplayStore();
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new ReplayOptions
        {
            SourceTopic = "",
            TargetTopic = "tgt",
        });

        var replayer = new MessageReplayer(
            store, producer, options, NullLogger<MessageReplayer>.Instance);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None));
    }

    // ── ReplayResult Timestamps Are Populated ────────────────────────────────

    [Test]
    public async Task Replay_Result_HasValidTimestamps()
    {
        var store = new InMemoryMessageReplayStore();
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new ReplayOptions
        {
            SourceTopic = "src",
            TargetTopic = "tgt",
            MaxMessages = 10,
        });

        var replayer = new MessageReplayer(
            store, producer, options, NullLogger<MessageReplayer>.Instance);

        var before = DateTimeOffset.UtcNow;
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.StartedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(result.CompletedAt, Is.GreaterThanOrEqualTo(result.StartedAt));
    }

    // ── ReplayFilter Record Shape ────────────────────────────────────────────

    [Test]
    public void ReplayFilter_DefaultValues_AreNull()
    {
        var filter = new ReplayFilter();

        Assert.That(filter.CorrelationId, Is.Null);
        Assert.That(filter.MessageType, Is.Null);
        Assert.That(filter.FromTimestamp, Is.Null);
        Assert.That(filter.ToTimestamp, Is.Null);
    }
}
