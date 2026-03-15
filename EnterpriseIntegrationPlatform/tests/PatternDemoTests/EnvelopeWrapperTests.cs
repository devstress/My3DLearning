using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Envelope Wrapper and Canonical Data Model patterns.
/// All messages use IntegrationEnvelope&lt;T&gt; as the canonical wrapper.
/// BizTalk equivalent: MessageBox context properties, promoted properties.
/// EIP: Envelope Wrapper (p. 330), Canonical Data Model (p. 355)
/// </summary>
public class EnvelopeWrapperTests
{
    private record OrderPayload(string OrderId, decimal Total);

    [Fact]
    public void Envelope_WrapsPayload_WithStandardMetadata()
    {
        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            new OrderPayload("ORD-001", 500),
            source: "ERP",
            messageType: "OrderCreated");

        envelope.MessageId.Should().NotBeEmpty();
        envelope.CorrelationId.Should().NotBeEmpty();
        envelope.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        envelope.Source.Should().Be("ERP");
        envelope.MessageType.Should().Be("OrderCreated");
        envelope.SchemaVersion.Should().Be("1.0");
        envelope.Priority.Should().Be(MessagePriority.Normal);
        envelope.Payload.OrderId.Should().Be("ORD-001");
    }

    [Fact]
    public void Envelope_SupportsCausation_ForMessageChaining()
    {
        var parentId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var child = IntegrationEnvelope<OrderPayload>.Create(
            new OrderPayload("ORD-002", 100),
            source: "Processor",
            messageType: "OrderValidated",
            correlationId: correlationId,
            causationId: parentId);

        child.CausationId.Should().Be(parentId);
        child.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void Envelope_SupportsCustomMetadata()
    {
        var envelope = new IntegrationEnvelope<OrderPayload>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "ERP",
            MessageType = "OrderCreated",
            Payload = new OrderPayload("ORD-003", 200),
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.ContentType] = "application/json",
                [MessageHeaders.SchemaVersion] = "2.0",
                [MessageHeaders.SourceTopic] = "orders.created",
            },
        };

        envelope.Metadata.Should().ContainKey(MessageHeaders.ContentType);
        envelope.Metadata[MessageHeaders.SourceTopic].Should().Be("orders.created");
    }
}
