using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class MessageLifecycleRecorderTests
{
    private readonly InMemoryMessageStateStore _store = new();
    private readonly MessageLifecycleRecorder _recorder;

    public MessageLifecycleRecorderTests()
    {
        _recorder = new MessageLifecycleRecorder(
            _store,
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
    public async Task RecordReceivedAsync_StoresEventWithPendingStatus()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordReceivedAsync(envelope, businessKey: "order-02");

        var events = await _store.GetByBusinessKeyAsync("order-02");
        events.Should().ContainSingle();
        events[0].Stage.Should().Be(MessageTracer.StageIngestion);
        events[0].Status.Should().Be(DeliveryStatus.Pending);
        events[0].BusinessKey.Should().Be("order-02");
    }

    [Fact]
    public async Task RecordProcessingAsync_StoresEventWithInFlightStatus()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, "order-02");

        var events = await _store.GetByBusinessKeyAsync("order-02");
        events.Should().ContainSingle();
        events[0].Stage.Should().Be(MessageTracer.StageRouting);
        events[0].Status.Should().Be(DeliveryStatus.InFlight);
    }

    [Fact]
    public async Task RecordDeliveredAsync_StoresDeliveredEvent()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordDeliveredAsync(envelope, activity: null, durationMs: 42.5, businessKey: "order-02");

        var events = await _store.GetByBusinessKeyAsync("order-02");
        events.Should().ContainSingle();
        events[0].Status.Should().Be(DeliveryStatus.Delivered);
        events[0].Details.Should().Contain("42.5ms");
    }

    [Fact]
    public async Task RecordFailedAsync_StoresFailedEventWithErrorMessage()
    {
        var envelope = CreateEnvelope();
        var exception = new InvalidOperationException("Connection refused");

        await _recorder.RecordFailedAsync(
            envelope, activity: null, exception, MessageTracer.StageDelivery, "order-02");

        var events = await _store.GetByBusinessKeyAsync("order-02");
        events.Should().ContainSingle();
        events[0].Status.Should().Be(DeliveryStatus.Failed);
        events[0].Details.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task RecordRetryAsync_StoresRetryingEvent()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordRetryAsync(envelope, retryCount: 3, MessageTracer.StageDelivery, "order-02");

        var events = await _store.GetByBusinessKeyAsync("order-02");
        events.Should().ContainSingle();
        events[0].Status.Should().Be(DeliveryStatus.Retrying);
        events[0].Details.Should().Contain("#3");
    }

    [Fact]
    public async Task RecordDeadLetteredAsync_StoresDeadLetteredEvent()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordDeadLetteredAsync(envelope, "Max retries exceeded", "order-02");

        var events = await _store.GetByBusinessKeyAsync("order-02");
        events.Should().ContainSingle();
        events[0].Status.Should().Be(DeliveryStatus.DeadLettered);
        events[0].Details.Should().Contain("Max retries exceeded");
    }

    [Fact]
    public async Task FullLifecycle_AllEventsRecorded_InOrder()
    {
        var envelope = CreateEnvelope();
        var correlationId = envelope.CorrelationId;

        await _recorder.RecordReceivedAsync(envelope, "order-02");
        await _recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, "order-02");
        await _recorder.RecordProcessingAsync(envelope, MessageTracer.StageTransformation, "order-02");
        await _recorder.RecordDeliveredAsync(envelope, null, 150.0, "order-02");

        var events = await _store.GetByCorrelationIdAsync(correlationId);
        events.Should().HaveCount(4);
        events[0].Status.Should().Be(DeliveryStatus.Pending);
        events[1].Status.Should().Be(DeliveryStatus.InFlight);
        events[2].Status.Should().Be(DeliveryStatus.InFlight);
        events[3].Status.Should().Be(DeliveryStatus.Delivered);
    }
}
