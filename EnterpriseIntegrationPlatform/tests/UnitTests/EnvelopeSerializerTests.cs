using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class EnvelopeSerializerTests
{
    [Test]
    public void Serialize_RoundTrips_StringPayload()
    {
        // Arrange
        var envelope = IntegrationEnvelope<string>.Create(
            "hello", "test-source", "test.message");

        // Act
        var bytes = EnvelopeSerializer.Serialize(envelope);
        var result = EnvelopeSerializer.Deserialize<string>(bytes);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(result.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(result.Source, Is.EqualTo("test-source"));
        Assert.That(result.MessageType, Is.EqualTo("test.message"));
        Assert.That(result.Payload, Is.EqualTo("hello"));
    }

    [Test]
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
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload.OrderId, Is.EqualTo("ORD-001"));
        Assert.That(result.Payload.Amount, Is.EqualTo(42.50m));
        Assert.That(result.Payload.Currency, Is.EqualTo("USD"));
    }

    [Test]
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
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Metadata.ContainsKey("tenant"), Is.True);
        Assert.That(result.Metadata["tenant"], Is.EqualTo("acme"));
        Assert.That(result.Metadata.ContainsKey("region"), Is.True);
        Assert.That(result.Metadata["region"], Is.EqualTo("eu-west-1"));
    }

    [Test]
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
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Priority, Is.EqualTo(MessagePriority.Critical));
    }

    [Test]
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
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CausationId, Is.EqualTo(parentId));
    }

    [Test]
    public void Deserialize_Throws_ForInvalidData()
    {
        // Arrange
        var invalidData = System.Text.Encoding.UTF8.GetBytes("not-valid-json");

        // Act
        var act = () => EnvelopeSerializer.Deserialize<string>(invalidData);

        // Assert — invalid JSON throws
        Assert.Throws<System.Text.Json.JsonException>(() => act());
    }

    private record OrderPayload(string OrderId, decimal Amount, string Currency);
}
