using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class MessageLifecycleRecorderTests
{
    private readonly InMemoryMessageStateStore _productionStore = new();
    private readonly IObservabilityEventLog _observabilityLog = Substitute.For<IObservabilityEventLog>();
    private readonly List<MessageEvent> _capturedObsEvents = [];
    private readonly MessageLifecycleRecorder _recorder;

    public MessageLifecycleRecorderTests()
    {
        _observabilityLog
            .RecordAsync(Arg.Any<MessageEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => _capturedObsEvents.Add(ci.Arg<MessageEvent>()));

        _recorder = new MessageLifecycleRecorder(
            _productionStore,
            _observabilityLog,
            NullLogger<MessageLifecycleRecorder>.Instance);
    }

    private static IntegrationEnvelope<string> CreateEnvelope(string messageType = "OrderShipment")
    {
        return IntegrationEnvelope<string>.Create(
            payload: "test-payload",
            source: "Gateway",
            messageType: messageType);
    }

    [Fact]
    public async Task RecordReceivedAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordReceivedAsync(envelope, businessKey: "order-02");

        // Production store receives the event
        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        prodEvents.Should().ContainSingle();
        prodEvents[0].Stage.Should().Be(MessageTracer.StageIngestion);
        prodEvents[0].Status.Should().Be(DeliveryStatus.Pending);

        // Observability store also receives the event
        _capturedObsEvents.Should().ContainSingle();
        _capturedObsEvents[0].Stage.Should().Be(MessageTracer.StageIngestion);
    }

    [Fact]
    public async Task RecordProcessingAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        prodEvents.Should().ContainSingle();
        prodEvents[0].Status.Should().Be(DeliveryStatus.InFlight);

        _capturedObsEvents.Should().ContainSingle();
        _capturedObsEvents[0].Status.Should().Be(DeliveryStatus.InFlight);
    }

    [Fact]
    public async Task RecordDeliveredAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordDeliveredAsync(envelope, activity: null, durationMs: 42.5, businessKey: "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        prodEvents.Should().ContainSingle();
        prodEvents[0].Status.Should().Be(DeliveryStatus.Delivered);

        _capturedObsEvents.Should().ContainSingle();
        _capturedObsEvents[0].Status.Should().Be(DeliveryStatus.Delivered);
    }

    [Fact]
    public async Task RecordFailedAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();
        var exception = new InvalidOperationException("Connection refused");

        await _recorder.RecordFailedAsync(
            envelope, activity: null, exception, MessageTracer.StageDelivery, "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        prodEvents.Should().ContainSingle();
        prodEvents[0].Status.Should().Be(DeliveryStatus.Failed);

        _capturedObsEvents.Should().ContainSingle();
        _capturedObsEvents[0].Details.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task RecordRetryAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordRetryAsync(envelope, retryCount: 3, MessageTracer.StageDelivery, "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        prodEvents.Should().ContainSingle();
        prodEvents[0].Status.Should().Be(DeliveryStatus.Retrying);

        _capturedObsEvents.Should().ContainSingle();
        _capturedObsEvents[0].Details.Should().Contain("#3");
    }

    [Fact]
    public async Task RecordDeadLetteredAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordDeadLetteredAsync(envelope, "Max retries exceeded", "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        prodEvents.Should().ContainSingle();
        prodEvents[0].Status.Should().Be(DeliveryStatus.DeadLettered);

        _capturedObsEvents.Should().ContainSingle();
        _capturedObsEvents[0].Details.Should().Contain("Max retries exceeded");
    }

    [Fact]
    public async Task FullLifecycle_AllEventsRecorded_InBothStores()
    {
        var envelope = CreateEnvelope();
        var correlationId = envelope.CorrelationId;

        await _recorder.RecordReceivedAsync(envelope, "order-02");
        await _recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, "order-02");
        await _recorder.RecordProcessingAsync(envelope, MessageTracer.StageTransformation, "order-02");
        await _recorder.RecordDeliveredAsync(envelope, null, 150.0, "order-02");

        // Production store has all events
        var prodEvents = await _productionStore.GetByCorrelationIdAsync(correlationId);
        prodEvents.Should().HaveCount(4);

        // Observability store also receives all events
        _capturedObsEvents.Should().HaveCount(4);
        _capturedObsEvents[0].Status.Should().Be(DeliveryStatus.Pending);
        _capturedObsEvents[3].Status.Should().Be(DeliveryStatus.Delivered);
    }
}
