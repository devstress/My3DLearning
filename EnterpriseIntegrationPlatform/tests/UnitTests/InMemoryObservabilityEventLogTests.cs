using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

/// <summary>
/// Smoke test for <see cref="InMemoryObservabilityEventLog"/>.
/// This in-memory implementation is a development-only fallback.
/// Full behavioural tests run against real Loki storage in the
/// IntegrationTests project.
/// </summary>
public class InMemoryObservabilityEventLogTests
{
    [Fact]
    public async Task RecordAndRetrieve_RoundTrips_Successfully()
    {
        var log = new InMemoryObservabilityEventLog();
        var correlationId = Guid.NewGuid();
        var evt = new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            MessageType = "OrderShipment",
            Source = "Gateway",
            Stage = "Ingestion",
            Status = DeliveryStatus.Pending,
            BusinessKey = "order-02",
        };

        await log.RecordAsync(evt);

        var byCorrelation = await log.GetByCorrelationIdAsync(correlationId);
        byCorrelation.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(evt);

        var byBusinessKey = await log.GetByBusinessKeyAsync("order-02");
        byBusinessKey.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(evt);
    }
}
