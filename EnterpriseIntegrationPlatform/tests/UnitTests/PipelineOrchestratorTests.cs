using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Observability;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PipelineOrchestratorTests
{
    private readonly IMessageRepository _repository;
    private readonly IMessageBrokerProducer _producer;
    private readonly ITemporalWorkflowDispatcher _dispatcher;
    private readonly PipelineOptions _options;
    private readonly PipelineOrchestrator _sut;

    public PipelineOrchestratorTests()
    {
        _repository = Substitute.For<IMessageRepository>();
        _producer = Substitute.For<IMessageBrokerProducer>();
        _dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        _options = new PipelineOptions();

        // Build a minimal MessageLifecycleRecorder with no-op dependencies
        var stateStore = Substitute.For<IMessageStateStore>();
        var eventLog = Substitute.For<IObservabilityEventLog>();
        var lifecycleLogger = NullLogger<MessageLifecycleRecorder>.Instance;
        var lifecycle = new MessageLifecycleRecorder(stateStore, eventLog, lifecycleLogger);

        _sut = new PipelineOrchestrator(
            _repository,
            lifecycle,
            _producer,
            _dispatcher,
            Options.Create(_options),
            NullLogger<PipelineOrchestrator>.Instance);
    }

    private static IntegrationEnvelope<JsonElement> BuildEnvelope(
        string payloadJson = """{"orderId":1}""")
    {
        var payload = JsonDocument.Parse(payloadJson).RootElement;
        return IntegrationEnvelope<JsonElement>.Create(
            payload,
            source: "TestSource",
            messageType: "OrderCreated");
    }

    [Test]
    public async Task ProcessAsync_ValidMessage_SavesMessageToCassandra()
    {
        var envelope = BuildEnvelope();
        _dispatcher.DispatchAsync(Arg.Any<ProcessIntegrationMessageInput>(), Arg.Any<string>())
            .Returns(new ProcessIntegrationMessageResult(envelope.MessageId, true));

        await _sut.ProcessAsync(envelope);

        await _repository.Received(1).SaveMessageAsync(
            Arg.Is<MessageRecord>(r => r.MessageId == envelope.MessageId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_ValidMessage_UpdatesStatusToDelivered()
    {
        var envelope = BuildEnvelope();
        _dispatcher.DispatchAsync(Arg.Any<ProcessIntegrationMessageInput>(), Arg.Any<string>())
            .Returns(new ProcessIntegrationMessageResult(envelope.MessageId, true));

        await _sut.ProcessAsync(envelope);

        await _repository.Received(1).UpdateDeliveryStatusAsync(
            envelope.MessageId,
            envelope.CorrelationId,
            Arg.Any<DateTimeOffset>(),
            DeliveryStatus.Delivered,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_ValidMessage_PublishesAckToCorrectSubject()
    {
        var envelope = BuildEnvelope();
        _dispatcher.DispatchAsync(Arg.Any<ProcessIntegrationMessageInput>(), Arg.Any<string>())
            .Returns(new ProcessIntegrationMessageResult(envelope.MessageId, true));

        await _sut.ProcessAsync(envelope);

        await _producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<AckPayload>>(),
            _options.AckSubject,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_InvalidMessage_UpdatesStatusToFailed()
    {
        var envelope = BuildEnvelope();
        _dispatcher.DispatchAsync(Arg.Any<ProcessIntegrationMessageInput>(), Arg.Any<string>())
            .Returns(new ProcessIntegrationMessageResult(envelope.MessageId, false, "Bad payload"));

        await _sut.ProcessAsync(envelope);

        await _repository.Received(1).UpdateDeliveryStatusAsync(
            envelope.MessageId,
            envelope.CorrelationId,
            Arg.Any<DateTimeOffset>(),
            DeliveryStatus.Failed,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_InvalidMessage_SavesFaultEnvelope()
    {
        var envelope = BuildEnvelope();
        _dispatcher.DispatchAsync(Arg.Any<ProcessIntegrationMessageInput>(), Arg.Any<string>())
            .Returns(new ProcessIntegrationMessageResult(envelope.MessageId, false, "Bad payload"));

        await _sut.ProcessAsync(envelope);

        await _repository.Received(1).SaveFaultAsync(
            Arg.Is<FaultEnvelope>(f =>
                f.OriginalMessageId == envelope.MessageId &&
                f.FaultReason == "Bad payload"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_InvalidMessage_PublishesNackToCorrectSubject()
    {
        var envelope = BuildEnvelope();
        _dispatcher.DispatchAsync(Arg.Any<ProcessIntegrationMessageInput>(), Arg.Any<string>())
            .Returns(new ProcessIntegrationMessageResult(envelope.MessageId, false, "Bad payload"));

        await _sut.ProcessAsync(envelope);

        await _producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<NackPayload>>(),
            _options.NackSubject,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_DispatcherThrows_SavesFaultEnvelope()
    {
        var envelope = BuildEnvelope();
        _dispatcher.DispatchAsync(Arg.Any<ProcessIntegrationMessageInput>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("Temporal unavailable"));

        await _sut.ProcessAsync(envelope);

        await _repository.Received(1).SaveFaultAsync(
            Arg.Is<FaultEnvelope>(f => f.OriginalMessageId == envelope.MessageId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_DispatcherThrows_PublishesNack()
    {
        var envelope = BuildEnvelope();
        _dispatcher.DispatchAsync(Arg.Any<ProcessIntegrationMessageInput>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("Temporal unavailable"));

        await _sut.ProcessAsync(envelope);

        await _producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<NackPayload>>(),
            _options.NackSubject,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_ValidMessage_DispatchesCorrectWorkflowInput()
    {
        var envelope = BuildEnvelope("""{"orderId":42}""");
        ProcessIntegrationMessageInput? capturedInput = null;

        _dispatcher.DispatchAsync(
                Arg.Do<ProcessIntegrationMessageInput>(i => capturedInput = i),
                Arg.Any<string>())
            .Returns(new ProcessIntegrationMessageResult(envelope.MessageId, true));

        await _sut.ProcessAsync(envelope);

        Assert.That(capturedInput, Is.Not.Null);
        Assert.That(capturedInput!.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(capturedInput.MessageType, Is.EqualTo(envelope.MessageType));
        Assert.That(capturedInput.PayloadJson, Does.Contain("42"));
    }

    [Test]
    public async Task ProcessAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var act = () => _sut.ProcessAsync(null!);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }
}
