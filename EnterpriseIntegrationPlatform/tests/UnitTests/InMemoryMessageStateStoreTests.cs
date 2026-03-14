using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class InMemoryMessageStateStoreTests
{
    private readonly InMemoryMessageStateStore _store = new();

    private static MessageEvent CreateEvent(
        Guid? correlationId = null,
        string businessKey = "order-02",
        string stage = "Ingestion",
        DeliveryStatus status = DeliveryStatus.Pending)
    {
        return new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId ?? Guid.NewGuid(),
            MessageType = "OrderShipment",
            Source = "Gateway",
            Stage = stage,
            Status = status,
            BusinessKey = businessKey,
            Details = $"Stage: {stage}",
        };
    }

    [Fact]
    public async Task RecordAsync_StoresEvent_GetByCorrelationIdReturnsIt()
    {
        var correlationId = Guid.NewGuid();
        var evt = CreateEvent(correlationId);

        await _store.RecordAsync(evt);

        var result = await _store.GetByCorrelationIdAsync(correlationId);
        result.Should().ContainSingle()
              .Which.Should().BeEquivalentTo(evt);
    }

    [Fact]
    public async Task GetByBusinessKeyAsync_ReturnsEventsForKey()
    {
        var correlationId = Guid.NewGuid();
        await _store.RecordAsync(CreateEvent(correlationId, "order-02", "Ingestion", DeliveryStatus.Pending));
        await _store.RecordAsync(CreateEvent(correlationId, "order-02", "Routing", DeliveryStatus.InFlight));

        var result = await _store.GetByBusinessKeyAsync("order-02");

        result.Should().HaveCount(2);
        result[0].Stage.Should().Be("Ingestion");
        result[1].Stage.Should().Be("Routing");
    }

    [Fact]
    public async Task GetByBusinessKeyAsync_IsCaseInsensitive()
    {
        var correlationId = Guid.NewGuid();
        await _store.RecordAsync(CreateEvent(correlationId, "Order-02"));

        var result = await _store.GetByBusinessKeyAsync("ORDER-02");

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByBusinessKeyAsync_ReturnsEmpty_WhenNotFound()
    {
        var result = await _store.GetByBusinessKeyAsync("nonexistent-key");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestByCorrelationIdAsync_ReturnsLatestEvent()
    {
        var correlationId = Guid.NewGuid();
        await _store.RecordAsync(CreateEvent(correlationId, stage: "Ingestion", status: DeliveryStatus.Pending));
        await _store.RecordAsync(CreateEvent(correlationId, stage: "Routing", status: DeliveryStatus.InFlight));
        await _store.RecordAsync(CreateEvent(correlationId, stage: "Delivery", status: DeliveryStatus.Delivered));

        var latest = await _store.GetLatestByCorrelationIdAsync(correlationId);

        latest.Should().NotBeNull();
        latest!.Stage.Should().Be("Delivery");
        latest.Status.Should().Be(DeliveryStatus.Delivered);
    }

    [Fact]
    public async Task GetLatestByCorrelationIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _store.GetLatestByCorrelationIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByMessageIdAsync_ReturnsEventsForMessage()
    {
        var messageId = Guid.NewGuid();
        var evt = new MessageEvent
        {
            MessageId = messageId,
            CorrelationId = Guid.NewGuid(),
            MessageType = "OrderShipment",
            Source = "Gateway",
            Stage = "Ingestion",
            Status = DeliveryStatus.Pending,
        };

        await _store.RecordAsync(evt);

        var result = await _store.GetByMessageIdAsync(messageId);

        result.Should().ContainSingle()
              .Which.MessageId.Should().Be(messageId);
    }

    [Fact]
    public async Task MultipleCorrelationIds_WithSameBusinessKey_ReturnsAllEvents()
    {
        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();
        await _store.RecordAsync(CreateEvent(corr1, "order-02", "Ingestion"));
        await _store.RecordAsync(CreateEvent(corr2, "order-02", "Ingestion"));

        var result = await _store.GetByBusinessKeyAsync("order-02");

        result.Should().HaveCount(2);
    }
}
