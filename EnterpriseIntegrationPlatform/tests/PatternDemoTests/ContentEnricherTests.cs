using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Content Enricher pattern.
/// Augments a message with additional data from an external source.
/// BizTalk equivalent: Orchestration shapes that call databases/APIs to enrich messages.
/// EIP: Content Enricher (p. 336)
/// </summary>
public class ContentEnricherTests
{
    private record CustomerOrder(string CustomerId, string OrderId, string? CustomerName, string? CreditRating);

    [Fact]
    public async Task Enriches_Order_WithCustomerDetails()
    {
        // Simulate looking up customer details from a database/API
        var enricher = new ContentEnricher<CustomerOrder>(
            async (order, ct) =>
            {
                // In production, this would call a customer service/database
                var name = order.CustomerId == "CUST-42" ? "Acme Corp" : "Unknown";
                var credit = order.CustomerId == "CUST-42" ? "AAA" : "N/A";
                return order with { CustomerName = name, CreditRating = credit };
            });

        var envelope = IntegrationEnvelope<CustomerOrder>.Create(
            new CustomerOrder("CUST-42", "ORD-001", null, null),
            "OrderService", "OrderCreated");

        var enriched = await enricher.EnrichAsync(envelope);

        enriched.Payload.CustomerName.Should().Be("Acme Corp");
        enriched.Payload.CreditRating.Should().Be("AAA");
        enriched.CorrelationId.Should().Be(envelope.CorrelationId);
    }
}
