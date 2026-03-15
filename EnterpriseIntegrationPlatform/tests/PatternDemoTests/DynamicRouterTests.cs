using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Dynamic Router pattern.
/// Routes messages based on rules that can be updated at runtime.
/// BizTalk equivalent: Business Rules Engine (BRE) integration for routing.
/// EIP: Dynamic Router (p. 243)
/// </summary>
public class DynamicRouterTests
{
    private record OrderPayload(string Region, string ProductType);

    [Fact]
    public void Routes_UsingConfiguredRules()
    {
        var router = new DynamicRouter<OrderPayload>();

        router.UpdateRules(new[]
        {
            new DynamicRoutingRule<OrderPayload>(
                e => e.Payload.Region == "APAC", "apac-processing", Priority: 1),
            new DynamicRoutingRule<OrderPayload>(
                e => e.Payload.ProductType == "Digital", "digital-fulfillment", Priority: 2),
        });

        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            new OrderPayload("US", "Digital"), "ERP", "OrderCreated");

        // Higher priority rule wins
        router.Route(envelope).Should().Be("digital-fulfillment");
    }

    [Fact]
    public void Routes_CanBeUpdated_AtRuntime()
    {
        var router = new DynamicRouter<OrderPayload>();

        // Initial rules
        router.UpdateRules(new[]
        {
            new DynamicRoutingRule<OrderPayload>(
                _ => true, "default-queue"),
        });

        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            new OrderPayload("US", "Physical"), "ERP", "OrderCreated");

        router.Route(envelope).Should().Be("default-queue");

        // Hot-swap rules without redeployment
        router.UpdateRules(new[]
        {
            new DynamicRoutingRule<OrderPayload>(
                _ => true, "new-queue"),
        });

        router.Route(envelope).Should().Be("new-queue");
    }
}
