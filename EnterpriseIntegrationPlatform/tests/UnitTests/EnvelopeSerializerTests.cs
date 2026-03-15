using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class EnvelopeSerializerTests
{
    [Fact]
    public void Serialize_RoundTrips_StringPayload()
    {
        // Arrange
        var envelope = IntegrationEnvelope<string>.Create(
            "hello", "test-source", "test.message");

        // Act
        var bytes = EnvelopeSerializer.Serialize(envelope);
        var result = EnvelopeSerializer.Deserialize<string>(bytes);

        // Assert
        result.Should().NotBeNull();
        result!.MessageId.Should().Be(envelope.MessageId);
        result.CorrelationId.Should().Be(envelope.CorrelationId);
        result.Source.Should().Be("test-source");
        result.MessageType.Should().Be("test.message");
        result.Payload.Should().Be("hello");
    }

    [Fact]
    public void Serialize_RoundTrips_ComplexPayload()
    {
        // Arrange
        var payload = new OrderPayload("ORD-001", 42.50m, "USD");
        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            payload, "order-service", "order.created");

        // Act
        var bytes = EnvelopeSerializer.Serialize(envelope);
        var result = EnvelopeSerializer.Deserialize<OrderPayload>(bytes);

        // Assert
        result.Should().NotBeNull();
        result!.Payload.OrderId.Should().Be("ORD-001");
        result.Payload.Amount.Should().Be(42.50m);
        result.Payload.Currency.Should().Be("USD");
    }

    [Fact]
    public void Serialize_PreservesMetadata()
    {
        // Arrange
        var envelope = IntegrationEnvelope<string>.Create(
            "test", "source", "type");
        envelope.Metadata["tenant"] = "acme";
        envelope.Metadata["region"] = "eu-west-1";

        // Act
        var bytes = EnvelopeSerializer.Serialize(envelope);
        var result = EnvelopeSerializer.Deserialize<string>(bytes);

        // Assert
        result.Should().NotBeNull();
        result!.Metadata.Should().ContainKey("tenant").WhoseValue.Should().Be("acme");
        result.Metadata.Should().ContainKey("region").WhoseValue.Should().Be("eu-west-1");
    }

    [Fact]
    public void Serialize_PreservesPriority()
    {
        // Arrange
        var envelope = IntegrationEnvelope<string>.Create(
            "urgent", "source", "type") with
        {
            Priority = MessagePriority.Critical,
        };

        // Act
        var bytes = EnvelopeSerializer.Serialize(envelope);
        var result = EnvelopeSerializer.Deserialize<string>(bytes);

        // Assert
        result.Should().NotBeNull();
        result!.Priority.Should().Be(MessagePriority.Critical);
    }

    [Fact]
    public void Serialize_PreservesCausationId()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var envelope = IntegrationEnvelope<string>.Create(
            "child", "source", "type", causationId: parentId);

        // Act
        var bytes = EnvelopeSerializer.Serialize(envelope);
        var result = EnvelopeSerializer.Deserialize<string>(bytes);

        // Assert
        result.Should().NotBeNull();
        result!.CausationId.Should().Be(parentId);
    }

    [Fact]
    public void Deserialize_Throws_ForInvalidData()
    {
        // Arrange
        var invalidData = System.Text.Encoding.UTF8.GetBytes("not-valid-json");

        // Act
        var act = () => EnvelopeSerializer.Deserialize<string>(invalidData);

        // Assert — invalid JSON throws
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    private record OrderPayload(string OrderId, decimal Amount, string Currency);
}
