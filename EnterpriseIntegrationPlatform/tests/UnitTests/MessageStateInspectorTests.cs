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
    private readonly InMemoryMessageStateStore _store = new();
    private readonly ITraceAnalyzer _analyzer = Substitute.For<ITraceAnalyzer>();
    private readonly MessageStateInspector _inspector;

    public MessageStateInspectorTests()
    {
        _inspector = new MessageStateInspector(
            _store,
            _analyzer,
            NullLogger<MessageStateInspector>.Instance);
    }

    [Fact]
    public async Task WhereIsAsync_ReturnsNotFound_WhenNoEventsExist()
    {
        var result = await _inspector.WhereIsAsync("order-99");

        result.Found.Should().BeFalse();
        result.Query.Should().Be("order-99");
        result.Summary.Should().Contain("No messages found");
    }

    [Fact]
    public async Task WhereIsAsync_ReturnsFound_WithAiSummary()
    {
        var correlationId = Guid.NewGuid();
        await _store.RecordAsync(new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            MessageType = "OrderShipment",
            Source = "Gateway",
            Stage = "Ingestion",
            Status = DeliveryStatus.Pending,
            BusinessKey = "order-02",
        });
        await _store.RecordAsync(new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            MessageType = "OrderShipment",
            Source = "Gateway",
            Stage = "Routing",
            Status = DeliveryStatus.InFlight,
            BusinessKey = "order-02",
        });

        _analyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("The shipment for order-02 is currently in the Routing stage.");

        var result = await _inspector.WhereIsAsync("order-02");

        result.Found.Should().BeTrue();
        result.Events.Should().HaveCount(2);
        result.LatestStage.Should().Be("Routing");
        result.LatestStatus.Should().Be(DeliveryStatus.InFlight);
        result.Summary.Should().Contain("Routing");
    }

    [Fact]
    public async Task WhereIsAsync_ReturnsFallbackSummary_WhenAiFails()
    {
        var correlationId = Guid.NewGuid();
        await _store.RecordAsync(new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            MessageType = "OrderShipment",
            Source = "Gateway",
            Stage = "Delivery",
            Status = DeliveryStatus.Delivered,
            BusinessKey = "order-03",
        });

        _analyzer.WhereIsMessageAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Ollama is down"));

        var result = await _inspector.WhereIsAsync("order-03");

        result.Found.Should().BeTrue();
        result.Summary.Should().Contain("order-03");
        result.Summary.Should().Contain("Delivery");
    }

    [Fact]
    public async Task WhereIsByCorrelationAsync_ReturnsEventsForCorrelationId()
    {
        var correlationId = Guid.NewGuid();
        await _store.RecordAsync(new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            MessageType = "InvoicePayment",
            Source = "Billing",
            Stage = "Ingestion",
            Status = DeliveryStatus.Pending,
        });

        _analyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Invoice is in Ingestion stage.");

        var result = await _inspector.WhereIsByCorrelationAsync(correlationId);

        result.Found.Should().BeTrue();
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
