using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class InMemoryObservabilityEventLogTests
{
    private readonly InMemoryObservabilityEventLog _log = new();

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

        await _log.RecordAsync(evt);

        var result = await _log.GetByCorrelationIdAsync(correlationId);
        result.Should().ContainSingle()
              .Which.Should().BeEquivalentTo(evt);
    }

    [Fact]
    public async Task GetByBusinessKeyAsync_ReturnsEventsForKey()
    {
        var correlationId = Guid.NewGuid();
        await _log.RecordAsync(CreateEvent(correlationId, "order-02", "Ingestion", DeliveryStatus.Pending));
        await _log.RecordAsync(CreateEvent(correlationId, "order-02", "Routing", DeliveryStatus.InFlight));

        var result = await _log.GetByBusinessKeyAsync("order-02");

        result.Should().HaveCount(2);
        result[0].Stage.Should().Be("Ingestion");
        result[1].Stage.Should().Be("Routing");
    }

    [Fact]
    public async Task GetByBusinessKeyAsync_IsCaseInsensitive()
    {
        var correlationId = Guid.NewGuid();
        await _log.RecordAsync(CreateEvent(correlationId, "Order-02"));

        var result = await _log.GetByBusinessKeyAsync("ORDER-02");

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByBusinessKeyAsync_ReturnsEmpty_WhenNotFound()
    {
        var result = await _log.GetByBusinessKeyAsync("nonexistent-key");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ReturnsEmpty_WhenNotFound()
    {
        var result = await _log.GetByCorrelationIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleCorrelationIds_WithSameBusinessKey_ReturnsAllEvents()
    {
        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();
        await _log.RecordAsync(CreateEvent(corr1, "order-02", "Ingestion"));
        await _log.RecordAsync(CreateEvent(corr2, "order-02", "Ingestion"));

        var result = await _log.GetByBusinessKeyAsync("order-02");

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task RecordAsync_WithoutBusinessKey_DoesNotIndex()
    {
        var correlationId = Guid.NewGuid();
        var evt = new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            MessageType = "Test",
            Source = "Gateway",
            Stage = "Ingestion",
            Status = DeliveryStatus.Pending,
            BusinessKey = null,
        };

        await _log.RecordAsync(evt);

        // Can still retrieve by correlation ID
        var result = await _log.GetByCorrelationIdAsync(correlationId);
        result.Should().ContainSingle();
    }

    [Fact]
    public async Task EventsAreOrderedByTimestamp()
    {
        var correlationId = Guid.NewGuid();
        var older = new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            MessageType = "Test",
            Source = "Gateway",
            Stage = "Ingestion",
            Status = DeliveryStatus.Pending,
            BusinessKey = "order-x",
            RecordedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
        var newer = new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            MessageType = "Test",
            Source = "Router",
            Stage = "Routing",
            Status = DeliveryStatus.InFlight,
            BusinessKey = "order-x",
            RecordedAt = DateTimeOffset.UtcNow,
        };

        // Insert in reverse order
        await _log.RecordAsync(newer);
        await _log.RecordAsync(older);

        var result = await _log.GetByCorrelationIdAsync(correlationId);
        result.Should().HaveCount(2);
        result[0].Stage.Should().Be("Ingestion");
        result[1].Stage.Should().Be("Routing");
    }
}
