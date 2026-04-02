using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class MessageStateInspectorTests
{
    private IObservabilityEventLog _observabilityLog = null!;
    private ITraceAnalyzer _analyzer = null!;
    private MessageStateInspector _inspector = null!;

    [SetUp]
    public void SetUp()
    {
        _observabilityLog = Substitute.For<IObservabilityEventLog>();
        _analyzer = Substitute.For<ITraceAnalyzer>();
        _inspector = new MessageStateInspector(
            _observabilityLog,
            _analyzer,
            NullLogger<MessageStateInspector>.Instance);
    }

    [Test]
    public async Task WhereIsAsync_ReturnsNotFound_WhenNoEventsExist()
    {
        _observabilityLog.GetByBusinessKeyAsync("order-99", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MessageEvent>());

        var result = await _inspector.WhereIsAsync("order-99");

        Assert.That(result.Found, Is.False);
        Assert.That(result.Query, Is.EqualTo("order-99"));
        Assert.That(result.Summary, Does.Contain("No messages found"));
    }

    [Test]
    public async Task WhereIsAsync_QueriesObservabilityLog_NotProductionStore()
    {
        var correlationId = Guid.NewGuid();
        var events = new List<MessageEvent>
        {
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "OrderShipment",
                Source = "Gateway",
                Stage = "Ingestion",
                Status = DeliveryStatus.Pending,
                BusinessKey = "order-02",
            },
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "OrderShipment",
                Source = "Gateway",
                Stage = "Routing",
                Status = DeliveryStatus.InFlight,
                BusinessKey = "order-02",
            },
        };

        _observabilityLog.GetByBusinessKeyAsync("order-02", Arg.Any<CancellationToken>())
            .Returns(events.AsReadOnly());

        _analyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("The shipment for order-02 is currently in the Routing stage.");

        var result = await _inspector.WhereIsAsync("order-02");

        Assert.That(result.Found, Is.True);
        Assert.That(result.OllamaAvailable, Is.True);
        Assert.That(result.Events, Has.Count.EqualTo(2));
        Assert.That(result.LatestStage, Is.EqualTo("Routing"));
        Assert.That(result.LatestStatus, Is.EqualTo(DeliveryStatus.InFlight));
        Assert.That(result.Summary, Does.Contain("Routing"));
    }

    [Test]
    public async Task WhereIsAsync_NotifiesOllamaUnavailable_WhenAiFails()
    {
        var correlationId = Guid.NewGuid();
        var events = new List<MessageEvent>
        {
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "OrderShipment",
                Source = "Gateway",
                Stage = "Delivery",
                Status = DeliveryStatus.Delivered,
                BusinessKey = "order-03",
            },
        };

        _observabilityLog.GetByBusinessKeyAsync("order-03", Arg.Any<CancellationToken>())
            .Returns(events.AsReadOnly());

        _analyzer.WhereIsMessageAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Ollama is down"));

        var result = await _inspector.WhereIsAsync("order-03");

        Assert.That(result.Found, Is.True);
        Assert.That(result.OllamaAvailable, Is.False);
        Assert.That(result.Summary, Does.Contain("Ollama is unavailable"));
        Assert.That(result.Events, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhereIsByCorrelationAsync_ReturnsEventsFromObservabilityLog()
    {
        var correlationId = Guid.NewGuid();
        var events = new List<MessageEvent>
        {
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "InvoicePayment",
                Source = "Billing",
                Stage = "Ingestion",
                Status = DeliveryStatus.Pending,
            },
        };

        _observabilityLog.GetByCorrelationIdAsync(correlationId, Arg.Any<CancellationToken>())
            .Returns(events.AsReadOnly());

        _analyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Invoice is in Ingestion stage.");

        var result = await _inspector.WhereIsByCorrelationAsync(correlationId);

        Assert.That(result.Found, Is.True);
        Assert.That(result.OllamaAvailable, Is.True);
        Assert.That(result.Events, Has.Count.EqualTo(1));
    }

    [Test]
    public void CreateSnapshot_BuildsCorrectSnapshot()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            payload: "test",
            source: "Gateway",
            messageType: "OrderShipment");

        var snapshot = _inspector.CreateSnapshot(envelope, "Routing", DeliveryStatus.InFlight);

        Assert.That(snapshot.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(snapshot.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(snapshot.MessageType, Is.EqualTo("OrderShipment"));
        Assert.That(snapshot.CurrentStage, Is.EqualTo("Routing"));
        Assert.That(snapshot.DeliveryStatus, Is.EqualTo(DeliveryStatus.InFlight));
    }
}
