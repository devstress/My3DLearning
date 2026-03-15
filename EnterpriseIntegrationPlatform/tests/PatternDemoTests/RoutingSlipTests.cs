using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Routing Slip pattern.
/// Attaches a sequence of processing steps to a message; each processor
/// handles its step and forwards to the next.
/// BizTalk equivalent: ESB Toolkit itinerary-based routing.
/// EIP: Routing Slip (p. 301)
/// </summary>
public class RoutingSlipTests
{
    [Fact]
    public void Advances_Through_Steps_InOrder()
    {
        var slip = new RoutingSlip<string>(new[]
        {
            new RoutingSlipStep("validate"),
            new RoutingSlipStep("transform"),
            new RoutingSlipStep("route"),
            new RoutingSlipStep("deliver"),
        });

        slip.CurrentStep!.Destination.Should().Be("validate");
        slip.IsComplete.Should().BeFalse();

        slip.Advance();
        slip.CurrentStep!.Destination.Should().Be("transform");

        slip.Advance();
        slip.CurrentStep!.Destination.Should().Be("route");

        slip.Advance();
        slip.CurrentStep!.Destination.Should().Be("deliver");

        slip.Advance();
        slip.IsComplete.Should().BeTrue();
        slip.CurrentStep.Should().BeNull();
    }

    [Fact]
    public void Steps_CanCarryMetadata()
    {
        var slip = new RoutingSlip<string>(new[]
        {
            new RoutingSlipStep("transform", new Dictionary<string, string>
            {
                ["MapName"] = "OrderToInvoice",
                ["Version"] = "2.0",
            }),
        });

        var step = slip.CurrentStep!;
        step.Metadata.Should().ContainKey("MapName").WhoseValue.Should().Be("OrderToInvoice");
    }
}
