using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
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

    [Test]
    public async Task RecordReceivedAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordReceivedAsync(envelope, businessKey: "order-02");

        // Production store receives the event
        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        Assert.That(prodEvents, Has.Count.EqualTo(1));
        Assert.That(prodEvents[0].Stage, Is.EqualTo(MessageTracer.StageIngestion));
        Assert.That(prodEvents[0].Status, Is.EqualTo(DeliveryStatus.Pending));

        // Observability store also receives the event
        Assert.That(_capturedObsEvents, Has.Count.EqualTo(1));
        Assert.That(_capturedObsEvents[0].Stage, Is.EqualTo(MessageTracer.StageIngestion));
    }

    [Test]
    public async Task RecordProcessingAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        Assert.That(prodEvents, Has.Count.EqualTo(1));
        Assert.That(prodEvents[0].Status, Is.EqualTo(DeliveryStatus.InFlight));

        Assert.That(_capturedObsEvents, Has.Count.EqualTo(1));
        Assert.That(_capturedObsEvents[0].Status, Is.EqualTo(DeliveryStatus.InFlight));
    }

    [Test]
    public async Task RecordDeliveredAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordDeliveredAsync(envelope, activity: null, durationMs: 42.5, businessKey: "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        Assert.That(prodEvents, Has.Count.EqualTo(1));
        Assert.That(prodEvents[0].Status, Is.EqualTo(DeliveryStatus.Delivered));

        Assert.That(_capturedObsEvents, Has.Count.EqualTo(1));
        Assert.That(_capturedObsEvents[0].Status, Is.EqualTo(DeliveryStatus.Delivered));
    }

    [Test]
    public async Task RecordFailedAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();
        var exception = new InvalidOperationException("Connection refused");

        await _recorder.RecordFailedAsync(
            envelope, activity: null, exception, MessageTracer.StageDelivery, "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        Assert.That(prodEvents, Has.Count.EqualTo(1));
        Assert.That(prodEvents[0].Status, Is.EqualTo(DeliveryStatus.Failed));

        Assert.That(_capturedObsEvents, Has.Count.EqualTo(1));
        Assert.That(_capturedObsEvents[0].Details, Does.Contain("Connection refused"));
    }

    [Test]
    public async Task RecordRetryAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordRetryAsync(envelope, retryCount: 3, MessageTracer.StageDelivery, "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        Assert.That(prodEvents, Has.Count.EqualTo(1));
        Assert.That(prodEvents[0].Status, Is.EqualTo(DeliveryStatus.Retrying));

        Assert.That(_capturedObsEvents, Has.Count.EqualTo(1));
        Assert.That(_capturedObsEvents[0].Details, Does.Contain("#3"));
    }

    [Test]
    public async Task RecordDeadLetteredAsync_WritesToBothStores()
    {
        var envelope = CreateEnvelope();

        await _recorder.RecordDeadLetteredAsync(envelope, "Max retries exceeded", "order-02");

        var prodEvents = await _productionStore.GetByBusinessKeyAsync("order-02");
        Assert.That(prodEvents, Has.Count.EqualTo(1));
        Assert.That(prodEvents[0].Status, Is.EqualTo(DeliveryStatus.DeadLettered));

        Assert.That(_capturedObsEvents, Has.Count.EqualTo(1));
        Assert.That(_capturedObsEvents[0].Details, Does.Contain("Max retries exceeded"));
    }

    [Test]
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
        Assert.That(prodEvents, Has.Count.EqualTo(4));

        // Observability store also receives all events
        Assert.That(_capturedObsEvents, Has.Count.EqualTo(4));
        Assert.That(_capturedObsEvents[0].Status, Is.EqualTo(DeliveryStatus.Pending));
        Assert.That(_capturedObsEvents[3].Status, Is.EqualTo(DeliveryStatus.Delivered));
    }
}
