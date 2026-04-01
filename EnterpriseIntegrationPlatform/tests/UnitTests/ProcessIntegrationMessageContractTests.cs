using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class ProcessIntegrationMessageContractTests
{
    [Test]
    public void ProcessIntegrationMessageInput_StoresAllProperties()
    {
        var messageId = Guid.NewGuid();
        var input = new ProcessIntegrationMessageInput(messageId, "OrderCreated", """{"id":1}""");

        Assert.That(input.MessageId, Is.EqualTo(messageId));
        Assert.That(input.MessageType, Is.EqualTo("OrderCreated"));
        Assert.That(input.PayloadJson, Is.EqualTo("""{"id":1}"""));
    }

    [Test]
    public void ProcessIntegrationMessageResult_ValidResult_HasIsValidTrue()
    {
        var messageId = Guid.NewGuid();
        var result = new ProcessIntegrationMessageResult(messageId, true);

        Assert.That(result.MessageId, Is.EqualTo(messageId));
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FailureReason, Is.Null);
    }

    [Test]
    public void ProcessIntegrationMessageResult_FailureResult_HasFailureReason()
    {
        var messageId = Guid.NewGuid();
        var result = new ProcessIntegrationMessageResult(messageId, false, "Payload is not valid JSON.");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Payload is not valid JSON."));
    }

    [Test]
    public void AckPayload_StoresAllProperties()
    {
        var msgId = Guid.NewGuid();
        var corrId = Guid.NewGuid();
        var ack = new AckPayload(msgId, corrId, "Delivered");

        Assert.That(ack.OriginalMessageId, Is.EqualTo(msgId));
        Assert.That(ack.CorrelationId, Is.EqualTo(corrId));
        Assert.That(ack.Outcome, Is.EqualTo("Delivered"));
    }

    [Test]
    public void NackPayload_StoresAllProperties()
    {
        var msgId = Guid.NewGuid();
        var corrId = Guid.NewGuid();
        var nack = new NackPayload(msgId, corrId, "Validation failed");

        Assert.That(nack.OriginalMessageId, Is.EqualTo(msgId));
        Assert.That(nack.CorrelationId, Is.EqualTo(corrId));
        Assert.That(nack.Reason, Is.EqualTo("Validation failed"));
    }
}
