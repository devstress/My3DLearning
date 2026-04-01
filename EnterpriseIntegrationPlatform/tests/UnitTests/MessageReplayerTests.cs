using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Replay;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class MessageReplayerTests
{
    private readonly IMessageReplayStore _store;
    private readonly IMessageBrokerProducer _producer;

    public MessageReplayerTests()
    {
        _store = Substitute.For<IMessageReplayStore>();
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private MessageReplayer BuildReplayer(ReplayOptions? options = null)
    {
        options ??= new ReplayOptions { SourceTopic = "source.topic", TargetTopic = "target.topic", MaxMessages = 100 };
        return new MessageReplayer(
            _store,
            _producer,
            Options.Create(options),
            NullLogger<MessageReplayer>.Instance);
    }

    private static IntegrationEnvelope<object> BuildObjectEnvelope(Guid? correlationId = null, string messageType = "TestEvent")
    {
        return new IntegrationEnvelope<object>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId ?? Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "TestService",
            MessageType = messageType,
            Payload = new object()
        };
    }

    private static async IAsyncEnumerable<IntegrationEnvelope<object>> ToAsyncEnumerable(
        IEnumerable<IntegrationEnvelope<object>> items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.CompletedTask;
        }
    }

    [Test]
    public async Task ReplayAsync_ValidMessages_RepublishesToTargetTopic()
    {
        var replayer = BuildReplayer();
        var envelope = BuildObjectEnvelope();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([envelope]));
        string? capturedTopic = null;
        await _producer.PublishAsync(Arg.Any<IntegrationEnvelope<object>>(), Arg.Do<string>(t => capturedTopic = t), Arg.Any<CancellationToken>());

        await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(capturedTopic, Is.EqualTo("target.topic"));
    }

    [Test]
    public async Task ReplayAsync_ReplayedEnvelope_GetsNewMessageId()
    {
        var replayer = BuildReplayer();
        var envelope = BuildObjectEnvelope();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([envelope]));
        IntegrationEnvelope<object>? captured = null;
        await _producer.PublishAsync(Arg.Do<IntegrationEnvelope<object>>(e => captured = e), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(captured!.MessageId, Is.Not.EqualTo(envelope.MessageId));
        Assert.That(captured!.MessageId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task ReplayAsync_ReplayedEnvelope_SetsCausationIdToOriginalMessageId()
    {
        var replayer = BuildReplayer();
        var envelope = BuildObjectEnvelope();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([envelope]));
        IntegrationEnvelope<object>? captured = null;
        await _producer.PublishAsync(Arg.Do<IntegrationEnvelope<object>>(e => captured = e), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(captured!.CausationId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task ReplayAsync_ThreeMessages_ReturnsCorrectCount()
    {
        var replayer = BuildReplayer();
        var envelopes = Enumerable.Range(0, 3).Select(_ => BuildObjectEnvelope()).ToList();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(envelopes));

        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(3));
    }

    [Test]
    public async Task ReplayAsync_EmptyFilter_ReplaysAllMessages()
    {
        var replayer = BuildReplayer();
        var envelopes = Enumerable.Range(0, 5).Select(_ => BuildObjectEnvelope()).ToList();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(envelopes));

        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(5));
    }

    [Test]
    public async Task ReplayAsync_CorrelationIdFilter_PassesFilterToStore()
    {
        var replayer = BuildReplayer();
        var correlationId = Guid.NewGuid();
        var filter = new ReplayFilter { CorrelationId = correlationId };
        ReplayFilter? capturedFilter = null;
        _store.GetMessagesForReplayAsync(
            Arg.Any<string>(),
            Arg.Do<ReplayFilter>(f => capturedFilter = f),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([]));

        await replayer.ReplayAsync(filter, CancellationToken.None);

        Assert.That(capturedFilter!.CorrelationId, Is.EqualTo(correlationId));
    }

    [Test]
    public async Task ReplayAsync_NoMessagesMatchFilter_ReturnsZeroCount()
    {
        var replayer = BuildReplayer();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([]));

        var result = await replayer.ReplayAsync(new ReplayFilter { CorrelationId = Guid.NewGuid() }, CancellationToken.None);

        Assert.That(result.ReplayedCount, Is.EqualTo(0));
    }

    [Test]
    public async Task ReplayAsync_EmptySourceTopic_ThrowsInvalidOperationException()
    {
        var replayer = BuildReplayer(new ReplayOptions { SourceTopic = "", TargetTopic = "target" });

        var act = async () => await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await act());
    }

    [Test]
    public async Task ReplayAsync_EmptyTargetTopic_ThrowsInvalidOperationException()
    {
        var replayer = BuildReplayer(new ReplayOptions { SourceTopic = "source", TargetTopic = "" });

        var act = async () => await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await act());
    }

    [Test]
    public async Task ReplayAsync_PublishesEnvelope_EnvelopeIsPublished()
    {
        var replayer = BuildReplayer();
        var envelope = BuildObjectEnvelope();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([envelope]));

        await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        await _producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<object>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
