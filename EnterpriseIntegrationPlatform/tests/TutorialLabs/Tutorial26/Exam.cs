// ============================================================================
// Tutorial 26 – Message Replay (Exam)
// ============================================================================
// Coding challenges: verify replay-id metadata injection, filter by
// CorrelationId, and confirm ReplayOptions default values.
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
public sealed class Exam
{
    // ── Challenge 1: Replayed Envelope Carries replay-id Metadata ────────────

    [Test]
    public async Task Challenge1_ReplayedEnvelope_ContainsReplayIdHeader()
    {
        IntegrationEnvelope<object>? captured = null;
        var store = new InMemoryMessageReplayStore();
        var producer = Substitute.For<IMessageBrokerProducer>();
        producer
            .PublishAsync(
                Arg.Do<IntegrationEnvelope<object>>(e => captured = e),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var options = Options.Create(new ReplayOptions
        {
            SourceTopic = "src",
            TargetTopic = "tgt",
            MaxMessages = 10,
        });

        var replayer = new MessageReplayer(
            store, producer, options, NullLogger<MessageReplayer>.Instance);

        var env = IntegrationEnvelope<string>.Create("data", "Svc", "type");
        await store.StoreForReplayAsync(env, "src", CancellationToken.None);

        await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Metadata.ContainsKey(MessageHeaders.ReplayId), Is.True);
        Assert.That(Guid.TryParse(captured.Metadata[MessageHeaders.ReplayId], out _), Is.True);
    }

    // ── Challenge 2: Filter By CorrelationId ────────────────────────────────

    [Test]
    public async Task Challenge2_FilterByCorrelationId_ReturnsOnlyMatchingMessages()
    {
        var store = new InMemoryMessageReplayStore();
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new ReplayOptions
        {
            SourceTopic = "src",
            TargetTopic = "tgt",
            MaxMessages = 100,
        });

        var replayer = new MessageReplayer(
            store, producer, options, NullLogger<MessageReplayer>.Instance);

        var targetCorrelation = Guid.NewGuid();
        var match = IntegrationEnvelope<string>.Create(
            "match", "Svc", "type", correlationId: targetCorrelation);
        var noMatch = IntegrationEnvelope<string>.Create("no", "Svc", "type");

        await store.StoreForReplayAsync(match, "src", CancellationToken.None);
        await store.StoreForReplayAsync(noMatch, "src", CancellationToken.None);

        var filter = new ReplayFilter { CorrelationId = targetCorrelation };
        var result = await replayer.ReplayAsync(filter, CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(1));
    }

    // ── Challenge 3: ReplayOptions Default Values ───────────────────────────

    [Test]
    public void Challenge3_ReplayOptions_DefaultValues()
    {
        var opts = new ReplayOptions();

        Assert.That(opts.SourceTopic, Is.EqualTo(string.Empty));
        Assert.That(opts.TargetTopic, Is.EqualTo(string.Empty));
        Assert.That(opts.MaxMessages, Is.EqualTo(1000));
        Assert.That(opts.BatchSize, Is.EqualTo(100));
        Assert.That(opts.SkipAlreadyReplayed, Is.False);
    }
}
