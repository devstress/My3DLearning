using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Replay;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

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

    [Fact]
    public async Task ReplayAsync_ValidMessages_RepublishesToTargetTopic()
    {
        var replayer = BuildReplayer();
        var envelope = BuildObjectEnvelope();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([envelope]));
        string? capturedTopic = null;
        await _producer.PublishAsync(Arg.Any<IntegrationEnvelope<object>>(), Arg.Do<string>(t => capturedTopic = t), Arg.Any<CancellationToken>());

        await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        capturedTopic.Should().Be("target.topic");
    }

    [Fact]
    public async Task ReplayAsync_ReplayedEnvelope_GetsNewMessageId()
    {
        var replayer = BuildReplayer();
        var envelope = BuildObjectEnvelope();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([envelope]));
        IntegrationEnvelope<object>? captured = null;
        await _producer.PublishAsync(Arg.Do<IntegrationEnvelope<object>>(e => captured = e), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        captured!.MessageId.Should().NotBe(envelope.MessageId);
        captured!.MessageId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task ReplayAsync_ReplayedEnvelope_SetsCausationIdToOriginalMessageId()
    {
        var replayer = BuildReplayer();
        var envelope = BuildObjectEnvelope();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([envelope]));
        IntegrationEnvelope<object>? captured = null;
        await _producer.PublishAsync(Arg.Do<IntegrationEnvelope<object>>(e => captured = e), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        captured!.CausationId.Should().Be(envelope.MessageId);
    }

    [Fact]
    public async Task ReplayAsync_ThreeMessages_ReturnsCorrectCount()
    {
        var replayer = BuildReplayer();
        var envelopes = Enumerable.Range(0, 3).Select(_ => BuildObjectEnvelope()).ToList();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(envelopes));

        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        result.ReplayedCount.Should().Be(3);
    }

    [Fact]
    public async Task ReplayAsync_EmptyFilter_ReplaysAllMessages()
    {
        var replayer = BuildReplayer();
        var envelopes = Enumerable.Range(0, 5).Select(_ => BuildObjectEnvelope()).ToList();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(envelopes));

        var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        result.ReplayedCount.Should().Be(5);
    }

    [Fact]
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

        capturedFilter!.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public async Task ReplayAsync_NoMessagesMatchFilter_ReturnsZeroCount()
    {
        var replayer = BuildReplayer();
        _store.GetMessagesForReplayAsync(Arg.Any<string>(), Arg.Any<ReplayFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([]));

        var result = await replayer.ReplayAsync(new ReplayFilter { CorrelationId = Guid.NewGuid() }, CancellationToken.None);

        result.ReplayedCount.Should().Be(0);
    }

    [Fact]
    public async Task ReplayAsync_EmptySourceTopic_ThrowsInvalidOperationException()
    {
        var replayer = BuildReplayer(new ReplayOptions { SourceTopic = "", TargetTopic = "target" });

        var act = async () => await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReplayAsync_EmptyTargetTopic_ThrowsInvalidOperationException()
    {
        var replayer = BuildReplayer(new ReplayOptions { SourceTopic = "source", TargetTopic = "" });

        var act = async () => await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
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
