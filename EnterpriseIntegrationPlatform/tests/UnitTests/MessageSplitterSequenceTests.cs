using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Splitter;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class MessageSplitterSequenceTests
{
    private IMessageBrokerProducer _producer = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private MessageSplitter<string> BuildSplitter(
        SplitterOptions options,
        Func<string, IReadOnlyList<string>>? splitFunc = null)
    {
        var strategy = new FuncSplitStrategy<string>(
            splitFunc ?? (s => s.Split(',').ToList()));

        return new MessageSplitter<string>(
            strategy,
            _producer,
            Options.Create(options),
            NullLogger<MessageSplitter<string>>.Instance);
    }

    private static IntegrationEnvelope<string> BuildEnvelope(
        string payload = "a,b,c",
        MessageIntent? intent = null,
        string? replyTo = null,
        DateTimeOffset? expiresAt = null) =>
        IntegrationEnvelope<string>.Create(payload, "svc", "BatchCreated") with
        {
            Intent = intent,
            ReplyTo = replyTo,
            ExpiresAt = expiresAt,
        };

    // ── SequenceNumber / TotalCount ───────────────────────────────────────────

    [Test]
    public async Task SplitAsync_SetsSequenceNumber_OnEachSplitEnvelope()
    {
        var sut = BuildSplitter(new SplitterOptions { TargetTopic = "out" });
        var envelope = BuildEnvelope(payload: "a,b,c");

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].SequenceNumber, Is.EqualTo(0));
        Assert.That(result.SplitEnvelopes[1].SequenceNumber, Is.EqualTo(1));
        Assert.That(result.SplitEnvelopes[2].SequenceNumber, Is.EqualTo(2));
    }

    [Test]
    public async Task SplitAsync_SetsTotalCount_OnEachSplitEnvelope()
    {
        var sut = BuildSplitter(new SplitterOptions { TargetTopic = "out" });
        var envelope = BuildEnvelope(payload: "a,b,c");

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].TotalCount, Is.EqualTo(3));
        Assert.That(result.SplitEnvelopes[1].TotalCount, Is.EqualTo(3));
        Assert.That(result.SplitEnvelopes[2].TotalCount, Is.EqualTo(3));
    }

    [Test]
    public async Task SplitAsync_SingleItem_SetsSequenceZeroAndTotalOne()
    {
        var sut = BuildSplitter(new SplitterOptions { TargetTopic = "out" }, s => [s]);
        var envelope = BuildEnvelope(payload: "single");

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].SequenceNumber, Is.EqualTo(0));
        Assert.That(result.SplitEnvelopes[0].TotalCount, Is.EqualTo(1));
    }

    // ── Intent preservation ───────────────────────────────────────────────────

    [Test]
    public async Task SplitAsync_PreservesIntent_FromSourceEnvelope()
    {
        var sut = BuildSplitter(new SplitterOptions { TargetTopic = "out" }, s => [s]);
        var envelope = BuildEnvelope(intent: MessageIntent.Command);

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].Intent, Is.EqualTo(MessageIntent.Command));
    }

    [Test]
    public async Task SplitAsync_PreservesNullIntent_FromSourceEnvelope()
    {
        var sut = BuildSplitter(new SplitterOptions { TargetTopic = "out" }, s => [s]);
        var envelope = BuildEnvelope(intent: null);

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].Intent, Is.Null);
    }

    // ── ReplyTo preservation ──────────────────────────────────────────────────

    [Test]
    public async Task SplitAsync_PreservesReplyTo_FromSourceEnvelope()
    {
        var sut = BuildSplitter(new SplitterOptions { TargetTopic = "out" }, s => [s]);
        var envelope = BuildEnvelope(replyTo: "replies.orders");

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].ReplyTo, Is.EqualTo("replies.orders"));
    }

    // ── ExpiresAt preservation ────────────────────────────────────────────────

    [Test]
    public async Task SplitAsync_PreservesExpiresAt_FromSourceEnvelope()
    {
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        var sut = BuildSplitter(new SplitterOptions { TargetTopic = "out" }, s => [s]);
        var envelope = BuildEnvelope(expiresAt: expiry);

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].ExpiresAt, Is.EqualTo(expiry));
    }
}
