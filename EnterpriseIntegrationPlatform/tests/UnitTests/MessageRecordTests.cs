using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class MessageRecordTests
{
    [Fact]
    public void DefaultPriority_IsNormal()
    {
        var record = new MessageRecord
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            RecordedAt = DateTimeOffset.UtcNow,
            Source = "Gateway",
            MessageType = "OrderShipment",
            PayloadJson = "{}",
        };

        record.Priority.Should().Be(MessagePriority.Normal);
    }

    [Fact]
    public void DefaultDeliveryStatus_IsPending()
    {
        var record = new MessageRecord
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            RecordedAt = DateTimeOffset.UtcNow,
            Source = "Gateway",
            MessageType = "OrderShipment",
            PayloadJson = "{}",
        };

        record.DeliveryStatus.Should().Be(DeliveryStatus.Pending);
    }

    [Fact]
    public void DefaultSchemaVersion_IsOnePointZero()
    {
        var record = new MessageRecord
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            RecordedAt = DateTimeOffset.UtcNow,
            Source = "Gateway",
            MessageType = "OrderShipment",
            PayloadJson = "{}",
        };

        record.SchemaVersion.Should().Be("1.0");
    }

    [Fact]
    public void CausationId_DefaultsToNull()
    {
        var record = new MessageRecord
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            RecordedAt = DateTimeOffset.UtcNow,
            Source = "Gateway",
            MessageType = "OrderShipment",
            PayloadJson = "{}",
        };

        record.CausationId.Should().BeNull();
    }

    [Fact]
    public void MetadataJson_DefaultsToNull()
    {
        var record = new MessageRecord
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            RecordedAt = DateTimeOffset.UtcNow,
            Source = "Gateway",
            MessageType = "OrderShipment",
            PayloadJson = "{}",
        };

        record.MetadataJson.Should().BeNull();
    }

    [Fact]
    public void AllRequiredProperties_CanBeSet()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var record = new MessageRecord
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            CausationId = causationId,
            RecordedAt = now,
            Source = "Connector.Http",
            MessageType = "OrderResponse",
            SchemaVersion = "2.0",
            Priority = MessagePriority.Critical,
            PayloadJson = """{"orderId":"ABC-123"}""",
            MetadataJson = """{"trace-id":"abc"}""",
            DeliveryStatus = DeliveryStatus.Delivered,
        };

        record.MessageId.Should().Be(messageId);
        record.CorrelationId.Should().Be(correlationId);
        record.CausationId.Should().Be(causationId);
        record.RecordedAt.Should().Be(now);
        record.Source.Should().Be("Connector.Http");
        record.MessageType.Should().Be("OrderResponse");
        record.SchemaVersion.Should().Be("2.0");
        record.Priority.Should().Be(MessagePriority.Critical);
        record.PayloadJson.Should().Be("""{"orderId":"ABC-123"}""");
        record.MetadataJson.Should().Be("""{"trace-id":"abc"}""");
        record.DeliveryStatus.Should().Be(DeliveryStatus.Delivered);
    }
}
