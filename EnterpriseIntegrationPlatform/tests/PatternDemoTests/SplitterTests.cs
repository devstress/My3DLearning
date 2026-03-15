using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Splitter pattern (also known as Debatching in BizTalk).
/// Breaks a composite message into individual messages.
/// BizTalk equivalent: Disassemble pipeline component, EDI batching/debatching.
/// EIP: Splitter (p. 259)
/// </summary>
public class SplitterTests
{
    private record OrderBatch(string BatchId, List<OrderItem> Items);
    private record OrderItem(string Sku, int Quantity);

    [Fact]
    public void Splits_BatchOrder_IntoIndividualItems()
    {
        var splitter = new MessageSplitter<OrderBatch, OrderItem>(
            batch => batch.Items);

        var batch = new OrderBatch("BATCH-001", new List<OrderItem>
        {
            new("SKU-A", 10),
            new("SKU-B", 5),
            new("SKU-C", 20),
        });

        var envelope = IntegrationEnvelope<OrderBatch>.Create(
            batch, "Warehouse", "OrderBatch");

        // Act
        var items = splitter.Split(envelope);

        // Assert
        items.Should().HaveCount(3);
        items.Select(i => i.Payload.Sku).Should().ContainInOrder("SKU-A", "SKU-B", "SKU-C");
    }

    [Fact]
    public void Split_Items_PreserveCorrelationId()
    {
        var splitter = new MessageSplitter<OrderBatch, OrderItem>(
            batch => batch.Items);

        var batch = new OrderBatch("BATCH-001", new List<OrderItem>
        {
            new("SKU-A", 10),
            new("SKU-B", 5),
        });

        var envelope = IntegrationEnvelope<OrderBatch>.Create(
            batch, "Warehouse", "OrderBatch");

        var items = splitter.Split(envelope);

        // All split items inherit the parent's correlation ID
        items.Should().AllSatisfy(i =>
            i.CorrelationId.Should().Be(envelope.CorrelationId));
    }

    [Fact]
    public void Split_Items_HaveCausationIdPointingToParent()
    {
        var splitter = new MessageSplitter<OrderBatch, OrderItem>(
            batch => batch.Items);

        var batch = new OrderBatch("BATCH-001", new List<OrderItem> { new("SKU-A", 10) });
        var envelope = IntegrationEnvelope<OrderBatch>.Create(
            batch, "Warehouse", "OrderBatch");

        var items = splitter.Split(envelope);

        // Each item's CausationId points back to the parent message
        items.Should().AllSatisfy(i =>
            i.CausationId.Should().Be(envelope.MessageId));
    }
}
