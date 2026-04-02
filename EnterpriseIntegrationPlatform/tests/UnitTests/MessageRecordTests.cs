using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class MessageRecordTests
{
    [Test]
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

        Assert.That(record.Priority, Is.EqualTo(MessagePriority.Normal));
    }

    [Test]
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

        Assert.That(record.DeliveryStatus, Is.EqualTo(DeliveryStatus.Pending));
    }

    [Test]
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

        Assert.That(record.SchemaVersion, Is.EqualTo("1.0"));
    }

    [Test]
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

        Assert.That(record.CausationId, Is.Null);
    }

    [Test]
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

        Assert.That(record.MetadataJson, Is.Null);
    }

    [Test]
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

        Assert.That(record.MessageId, Is.EqualTo(messageId));
        Assert.That(record.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(record.CausationId, Is.EqualTo(causationId));
        Assert.That(record.RecordedAt, Is.EqualTo(now));
        Assert.That(record.Source, Is.EqualTo("Connector.Http"));
        Assert.That(record.MessageType, Is.EqualTo("OrderResponse"));
        Assert.That(record.SchemaVersion, Is.EqualTo("2.0"));
        Assert.That(record.Priority, Is.EqualTo(MessagePriority.Critical));
        Assert.That(record.PayloadJson, Is.EqualTo("""{"orderId":"ABC-123"}"""));
        Assert.That(record.MetadataJson, Is.EqualTo("""{"trace-id":"abc"}"""));
        Assert.That(record.DeliveryStatus, Is.EqualTo(DeliveryStatus.Delivered));
    }
}
