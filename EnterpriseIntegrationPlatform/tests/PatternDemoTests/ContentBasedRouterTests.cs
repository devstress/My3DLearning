using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Content-Based Router pattern.
/// Routes messages to different destinations based on message content.
/// BizTalk equivalent: Filter expressions on Send Ports / Orchestration routing shapes.
/// EIP: Content-Based Router (p. 230)
/// </summary>
public class ContentBasedRouterTests
{
    private record OrderPayload(string Region, decimal Amount);

    [Fact]
    public void Routes_HighValueOrder_ToVipQueue()
    {
        // Arrange — configure routing rules
        var router = new ContentBasedRouter<OrderPayload>()
            .When(e => e.Payload.Amount > 10_000, "vip-orders")
            .When(e => e.Payload.Region == "EU", "eu-orders")
            .Otherwise("standard-orders");

        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            new OrderPayload("US", 50_000), "ERP", "OrderCreated");

        // Act
        var destinations = router.Route(envelope);

        // Assert — first matching rule wins
        destinations.Should().ContainSingle().Which.Should().Be("vip-orders");
    }

    [Fact]
    public void Routes_EuOrder_ToEuQueue()
    {
        var router = new ContentBasedRouter<OrderPayload>()
            .When(e => e.Payload.Amount > 10_000, "vip-orders")
            .When(e => e.Payload.Region == "EU", "eu-orders")
            .Otherwise("standard-orders");

        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            new OrderPayload("EU", 500), "ERP", "OrderCreated");

        var destinations = router.Route(envelope);

        destinations.Should().ContainSingle().Which.Should().Be("eu-orders");
    }

    [Fact]
    public void Routes_StandardOrder_ToDefaultQueue()
    {
        var router = new ContentBasedRouter<OrderPayload>()
            .When(e => e.Payload.Amount > 10_000, "vip-orders")
            .When(e => e.Payload.Region == "EU", "eu-orders")
            .Otherwise("standard-orders");

        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            new OrderPayload("US", 100), "ERP", "OrderCreated");

        var destinations = router.Route(envelope);

        destinations.Should().ContainSingle().Which.Should().Be("standard-orders");
    }
}
