using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Processing.Transform;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Normalizer pattern.
/// Converts messages from multiple source formats into a single canonical format.
/// BizTalk equivalent: Flat File Disassembler, custom pipeline components that
/// normalize EDI, XML, CSV, or JSON into the canonical schema.
/// EIP: Normalizer (p. 352)
/// </summary>
public class NormalizerTests
{
    private record CanonicalOrder(string OrderId, string Customer, decimal Total);

    // Source format from system A
    private record SystemAOrder(string Id, string CustomerName, double Amount);

    // Source format from system B
    private record SystemBOrder(string Ref, string Acct, int Cents);

    [Fact]
    public void Normalizes_DifferentFormats_ToCanonical()
    {
        var normalizer = new MessageNormalizer<CanonicalOrder>();

        // Register converters for different source formats
        normalizer.Register<SystemAOrder>("SystemA.Order",
            a => new CanonicalOrder(a.Id, a.CustomerName, (decimal)a.Amount));

        normalizer.Register<SystemBOrder>("SystemB.Order",
            b => new CanonicalOrder(b.Ref, b.Acct, b.Cents / 100m));

        // Normalize from system A
        var resultA = normalizer.Normalize(
            "SystemA.Order",
            new SystemAOrder("A-001", "Acme Corp", 1500.50),
            "SystemA");

        resultA.Payload.OrderId.Should().Be("A-001");
        resultA.Payload.Customer.Should().Be("Acme Corp");
        resultA.Payload.Total.Should().Be(1500.50m);

        // Normalize from system B
        var resultB = normalizer.Normalize(
            "SystemB.Order",
            new SystemBOrder("B-002", "Widget Inc", 250000),
            "SystemB");

        resultB.Payload.OrderId.Should().Be("B-002");
        resultB.Payload.Customer.Should().Be("Widget Inc");
        resultB.Payload.Total.Should().Be(2500.00m);
    }

    [Fact]
    public void Throws_WhenNoNormalizerRegistered()
    {
        var normalizer = new MessageNormalizer<CanonicalOrder>();

        var act = () => normalizer.Normalize(
            "Unknown.Format", new object(), "Unknown");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*normalizer*Unknown.Format*");
    }
}
