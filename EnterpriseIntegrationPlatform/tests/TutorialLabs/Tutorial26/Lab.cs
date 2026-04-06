// ============================================================================
// Tutorial 26 – Message Replay (Lab)
// ============================================================================
// EIP Pattern: Message Store / Replay.
// E2E: MessageReplayer with InMemoryMessageReplayStore + MockEndpoint.
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
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("replay-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    [Test]
    public async Task Replay_SingleMessage_PublishesToTargetTopic()
    {
        var store = new InMemoryMessageReplayStore();
        var envelope = IntegrationEnvelope<string>.Create("order-1", "OrderService", "order.created");
        await store.StoreForReplayAsync(envelope, "source-topic", CancellationToken.None);

        var replayer = CreateReplayer(store);
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(1));
        Assert.That(result.FailedCount, Is.EqualTo(0));
        _output.AssertReceivedOnTopic("replay-target", 1);
    }

    [Test]
    public async Task Replay_MultipleMessages_ReplaysAll()
    {
        var store = new InMemoryMessageReplayStore();
        for (var i = 0; i < 3; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"data-{i}", "Svc", "event.type");
            await store.StoreForReplayAsync(env, "source-topic", CancellationToken.None);
        }

        var replayer = CreateReplayer(store);
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(3));
        _output.AssertReceivedOnTopic("replay-target", 3);
    }

    [Test]
    public async Task Replay_FilterByMessageType_OnlyMatchingReplayed()
    {
        var store = new InMemoryMessageReplayStore();
        await store.StoreForReplayAsync(
            IntegrationEnvelope<string>.Create("a", "Svc", "order.created"), "source-topic", CancellationToken.None);
        await store.StoreForReplayAsync(
            IntegrationEnvelope<string>.Create("b", "Svc", "payment.received"), "source-topic", CancellationToken.None);

        var replayer = CreateReplayer(store);
        var filter = new ReplayFilter { MessageType = "order.created" };
        var result = await replayer.ReplayAsync(filter, CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(1));
        _output.AssertReceivedOnTopic("replay-target", 1);
    }

    [Test]
    public async Task Replay_EmptyStore_ReturnsZeroReplayed()
    {
        var store = new InMemoryMessageReplayStore();
        var replayer = CreateReplayer(store);
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(0));
        Assert.That(result.SkippedCount, Is.EqualTo(0));
        _output.AssertNoneReceived();
    }

    [Test]
    public async Task Replay_SkipAlreadyReplayed_SkipsTaggedMessages()
    {
        var store = new InMemoryMessageReplayStore();
        var env = IntegrationEnvelope<string>.Create("data", "Svc", "event") with
        {
            Metadata = new Dictionary<string, string> { [MessageHeaders.ReplayId] = Guid.NewGuid().ToString() },
        };
        await store.StoreForReplayAsync(env, "source-topic", CancellationToken.None);

        var opts = new ReplayOptions
        {
            SourceTopic = "source-topic",
            TargetTopic = "replay-target",
            MaxMessages = 100,
            SkipAlreadyReplayed = true,
        };
        var replayer = new MessageReplayer(store, _output, Options.Create(opts), NullLogger<MessageReplayer>.Instance);
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.SkippedCount, Is.EqualTo(1));
        Assert.That(result.ReplayedCount, Is.EqualTo(0));
        _output.AssertNoneReceived();
    }

    [Test]
    public async Task Replay_ResultTimestamps_ArePopulated()
    {
        var store = new InMemoryMessageReplayStore();
        await store.StoreForReplayAsync(
            IntegrationEnvelope<string>.Create("d", "Svc", "evt"), "source-topic", CancellationToken.None);

        var replayer = CreateReplayer(store);
        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.StartedAt, Is.LessThanOrEqualTo(result.CompletedAt));
        Assert.That(result.CompletedAt, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }

    private MessageReplayer CreateReplayer(InMemoryMessageReplayStore store)
    {
        var opts = Options.Create(new ReplayOptions
        {
            SourceTopic = "source-topic",
            TargetTopic = "replay-target",
            MaxMessages = 100,
        });
        return new MessageReplayer(store, _output, opts, NullLogger<MessageReplayer>.Instance);
    }
}
