using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Scatter-Gather pattern.
/// Sends a request to multiple handlers in parallel and aggregates results.
/// BizTalk equivalent: Parallel Convoy receiving responses from multiple systems.
/// EIP: Scatter-Gather (p. 297)
/// </summary>
public class ScatterGatherTests
{
    private record PriceRequest(string ProductId);
    private record PriceQuote(string Supplier, decimal Price);

    [Fact]
    public async Task Gathers_Quotes_FromMultipleSuppliers()
    {
        var scatterGather = new ScatterGather<PriceRequest, PriceQuote>()
            .AddHandler(async (env, ct) =>
            {
                await Task.Delay(10, ct);
                return new PriceQuote("SupplierA", 99.99m);
            })
            .AddHandler(async (env, ct) =>
            {
                await Task.Delay(5, ct);
                return new PriceQuote("SupplierB", 89.50m);
            })
            .AddHandler(async (env, ct) =>
            {
                await Task.Delay(15, ct);
                return new PriceQuote("SupplierC", 105.00m);
            });

        var request = IntegrationEnvelope<PriceRequest>.Create(
            new PriceRequest("WIDGET-42"), "Procurement", "PriceRequest");

        // Act — scatter to all suppliers, gather all results
        var quotes = await scatterGather.ScatterAsync(request);

        // Assert — all 3 suppliers responded
        quotes.Should().HaveCount(3);
        quotes.Min(q => q.Price).Should().Be(89.50m);
    }
}
