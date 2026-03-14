using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class MessageStateInspectorTests
{
    private readonly IObservabilityEventLog _observabilityLog = Substitute.For<IObservabilityEventLog>();
    private readonly ITraceAnalyzer _analyzer = Substitute.For<ITraceAnalyzer>();
    private readonly MessageStateInspector _inspector;

    public MessageStateInspectorTests()
    {
        _inspector = new MessageStateInspector(
            _observabilityLog,
            _analyzer,
            NullLogger<MessageStateInspector>.Instance);
    }

    [Fact]
    public async Task WhereIsAsync_ReturnsNotFound_WhenNoEventsExist()
    {
        _observabilityLog.GetByBusinessKeyAsync("order-99", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MessageEvent>());

        var result = await _inspector.WhereIsAsync("order-99");

        result.Found.Should().BeFalse();
        result.Query.Should().Be("order-99");
        result.Summary.Should().Contain("No messages found");
    }

    [Fact]
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

        result.Found.Should().BeTrue();
        result.OllamaAvailable.Should().BeTrue();
        result.Events.Should().HaveCount(2);
        result.LatestStage.Should().Be("Routing");
        result.LatestStatus.Should().Be(DeliveryStatus.InFlight);
        result.Summary.Should().Contain("Routing");
    }

    [Fact]
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

        result.Found.Should().BeTrue();
        result.OllamaAvailable.Should().BeFalse();
        result.Summary.Should().Contain("Ollama is unavailable");
        result.Events.Should().ContainSingle();
    }

    [Fact]
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

        result.Found.Should().BeTrue();
        result.OllamaAvailable.Should().BeTrue();
        result.Events.Should().ContainSingle();
    }

    [Fact]
    public void CreateSnapshot_BuildsCorrectSnapshot()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            payload: "test",
            source: "Gateway",
            messageType: "OrderShipment");

        var snapshot = _inspector.CreateSnapshot(envelope, "Routing", DeliveryStatus.InFlight);

        snapshot.MessageId.Should().Be(envelope.MessageId);
        snapshot.CorrelationId.Should().Be(envelope.CorrelationId);
        snapshot.MessageType.Should().Be("OrderShipment");
        snapshot.CurrentStage.Should().Be("Routing");
        snapshot.DeliveryStatus.Should().Be(DeliveryStatus.InFlight);
    }
}
