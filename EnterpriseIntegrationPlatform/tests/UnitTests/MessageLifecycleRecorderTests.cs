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
    private readonly InMemoryObservabilityEventLog _observabilityLog = new();
    private readonly MessageLifecycleRecorder _recorder;

    public MessageLifecycleRecorderTests()
    {
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
        var obsEvents = await _observabilityLog.GetByBusinessKeyAsync("order-02");
        obsEvents.Should().ContainSingle();
        obsEvents[0].Stage.Should().Be(MessageTracer.StageIngestion);
    }

    [Fact]
    public async Task RecordProcessingAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        prodEvents.Should().ContainSingle();
        prodEvents[0].Status.Should().Be(DeliveryStatus.InFlight);

        var obsEvents = await _observabilityLog.GetByBusinessKeyAsync("order-02");
        obsEvents.Should().ContainSingle();
        obsEvents[0].Status.Should().Be(DeliveryStatus.InFlight);
    }

    [Fact]
    public async Task RecordDeliveredAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordDeliveredAsync(envelope, activity: null, durationMs: 42.5, businessKey: "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        prodEvents.Should().ContainSingle();
        prodEvents[0].Status.Should().Be(DeliveryStatus.Delivered);

        var obsEvents = await _observabilityLog.GetByBusinessKeyAsync("order-02");
        obsEvents.Should().ContainSingle();
        obsEvents[0].Status.Should().Be(DeliveryStatus.Delivered);
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

        var obsEvents = await _observabilityLog.GetByBusinessKeyAsync("order-02");
        obsEvents.Should().ContainSingle();
        obsEvents[0].Details.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task RecordRetryAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordRetryAsync(envelope, retryCount: 3, MessageTracer.StageDelivery, "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        prodEvents.Should().ContainSingle();
        prodEvents[0].Status.Should().Be(DeliveryStatus.Retrying);

        var obsEvents = await _observabilityLog.GetByBusinessKeyAsync("order-02");
        obsEvents.Should().ContainSingle();
        obsEvents[0].Details.Should().Contain("#3");
    }

    [Fact]
    public async Task RecordDeadLetteredAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordDeadLetteredAsync(envelope, "Max retries exceeded", "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        prodEvents.Should().ContainSingle();
        prodEvents[0].Status.Should().Be(DeliveryStatus.DeadLettered);

        var obsEvents = await _observabilityLog.GetByBusinessKeyAsync("order-02");
        obsEvents.Should().ContainSingle();
        obsEvents[0].Details.Should().Contain("Max retries exceeded");
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

        // Observability store also has all events (isolated)
        var obsEvents = await _observabilityLog.GetByCorrelationIdAsync(correlationId);
        obsEvents.Should().HaveCount(4);
        obsEvents[0].Status.Should().Be(DeliveryStatus.Pending);
        obsEvents[3].Status.Should().Be(DeliveryStatus.Delivered);
    }
}
