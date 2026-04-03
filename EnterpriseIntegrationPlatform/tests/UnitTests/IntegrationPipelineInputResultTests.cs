using EnterpriseIntegrationPlatform.Activities;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class IntegrationPipelineInputTests
{
    [Test]
    public void Constructor_SetsAllProperties()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var input = new IntegrationPipelineInput(
            MessageId: messageId,
            CorrelationId: correlationId,
            CausationId: causationId,
            Timestamp: timestamp,
            Source: "TestSource",
            MessageType: "OrderCreated",
            SchemaVersion: "2.0",
            Priority: 3,
            PayloadJson: """{"id":1}""",
            MetadataJson: """{"key":"val"}""",
            AckSubject: "ack.topic",
            NackSubject: "nack.topic");

        Assert.That(input.MessageId, Is.EqualTo(messageId));
        Assert.That(input.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(input.CausationId, Is.EqualTo(causationId));
        Assert.That(input.Timestamp, Is.EqualTo(timestamp));
        Assert.That(input.Source, Is.EqualTo("TestSource"));
        Assert.That(input.MessageType, Is.EqualTo("OrderCreated"));
        Assert.That(input.SchemaVersion, Is.EqualTo("2.0"));
        Assert.That(input.Priority, Is.EqualTo(3));
        Assert.That(input.PayloadJson, Is.EqualTo("""{"id":1}"""));
        Assert.That(input.MetadataJson, Is.EqualTo("""{"key":"val"}"""));
        Assert.That(input.AckSubject, Is.EqualTo("ack.topic"));
        Assert.That(input.NackSubject, Is.EqualTo("nack.topic"));
    }

    [Test]
    public void Constructor_NullableCausationId_CanBeNull()
    {
        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            Timestamp: DateTimeOffset.UtcNow,
            Source: "Src",
            MessageType: "Type",
            SchemaVersion: "1.0",
            Priority: 0,
            PayloadJson: "{}",
            MetadataJson: null,
            AckSubject: "ack",
            NackSubject: "nack");

        Assert.That(input.CausationId, Is.Null);
        Assert.That(input.MetadataJson, Is.Null);
    }

    [Test]
    public void RecordEquality_SameValues_AreEqual()
    {
        var id = Guid.NewGuid();
        var corr = Guid.NewGuid();
        var ts = DateTimeOffset.UtcNow;

        var a = new IntegrationPipelineInput(id, corr, null, ts, "S", "T", "1.0", 1, "{}", null, "a", "n");
        var b = new IntegrationPipelineInput(id, corr, null, ts, "S", "T", "1.0", 1, "{}", null, "a", "n");

        Assert.That(a, Is.EqualTo(b));
    }
}

[TestFixture]
public class IntegrationPipelineResultTests
{
    [Test]
    public void Success_HasIsSuccessTrue()
    {
        var id = Guid.NewGuid();
        var result = new IntegrationPipelineResult(id, true);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.MessageId, Is.EqualTo(id));
        Assert.That(result.FailureReason, Is.Null);
    }

    [Test]
    public void Failure_HasIsSuccessFalseAndReason()
    {
        var id = Guid.NewGuid();
        var result = new IntegrationPipelineResult(id, false, "Bad payload");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Bad payload"));
    }

    [Test]
    public void RecordEquality_SameValues_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = new IntegrationPipelineResult(id, true);
        var b = new IntegrationPipelineResult(id, true);

        Assert.That(a, Is.EqualTo(b));
    }
}
