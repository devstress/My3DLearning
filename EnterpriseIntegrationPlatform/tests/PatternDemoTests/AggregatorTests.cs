using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Aggregator pattern (also known as Convoy in BizTalk).
/// Collects related messages and combines them into a single message.
/// BizTalk equivalent: Sequential Convoy, Parallel Convoy orchestration patterns.
/// EIP: Aggregator (p. 268)
/// </summary>
public class AggregatorTests
{
    private record ShipmentItem(string TrackingId, string Status);

    [Fact]
    public void Aggregates_CorrelatedMessages_WhenCountReached()
    {
        var correlationId = Guid.NewGuid();
        var aggregator = new CountBasedAggregator<ShipmentItem>(expectedCount: 3);

        // Simulate 3 correlated shipment updates arriving independently
        aggregator.Add(IntegrationEnvelope<ShipmentItem>.Create(
            new ShipmentItem("TRK-1", "Picked"), "WMS", "ShipmentUpdate", correlationId));

        aggregator.Add(IntegrationEnvelope<ShipmentItem>.Create(
            new ShipmentItem("TRK-2", "Packed"), "WMS", "ShipmentUpdate", correlationId));

        aggregator.IsComplete(correlationId).Should().BeFalse();

        aggregator.Add(IntegrationEnvelope<ShipmentItem>.Create(
            new ShipmentItem("TRK-3", "Shipped"), "WMS", "ShipmentUpdate", correlationId));

        // Assert — aggregation complete after 3rd message
        aggregator.IsComplete(correlationId).Should().BeTrue();

        var result = aggregator.Harvest(correlationId);
        result.Payload.Should().HaveCount(3);
        result.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void Keeps_Separate_Aggregations_ByCorrelationId()
    {
        var aggregator = new CountBasedAggregator<ShipmentItem>(expectedCount: 2);
        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();

        aggregator.Add(IntegrationEnvelope<ShipmentItem>.Create(
            new ShipmentItem("A", "Picked"), "WMS", "ShipmentUpdate", corr1));

        aggregator.Add(IntegrationEnvelope<ShipmentItem>.Create(
            new ShipmentItem("B", "Picked"), "WMS", "ShipmentUpdate", corr2));

        aggregator.IsComplete(corr1).Should().BeFalse();
        aggregator.IsComplete(corr2).Should().BeFalse();

        aggregator.Add(IntegrationEnvelope<ShipmentItem>.Create(
            new ShipmentItem("C", "Shipped"), "WMS", "ShipmentUpdate", corr1));

        aggregator.IsComplete(corr1).Should().BeTrue();
        aggregator.IsComplete(corr2).Should().BeFalse();
    }
}
