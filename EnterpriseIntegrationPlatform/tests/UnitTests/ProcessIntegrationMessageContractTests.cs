using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class ProcessIntegrationMessageContractTests
{
    [Fact]
    public void ProcessIntegrationMessageInput_StoresAllProperties()
    {
        var messageId = Guid.NewGuid();
        var input = new ProcessIntegrationMessageInput(messageId, "OrderCreated", """{"id":1}""");

        input.MessageId.Should().Be(messageId);
        input.MessageType.Should().Be("OrderCreated");
        input.PayloadJson.Should().Be("""{"id":1}""");
    }

    [Fact]
    public void ProcessIntegrationMessageResult_ValidResult_HasIsValidTrue()
    {
        var messageId = Guid.NewGuid();
        var result = new ProcessIntegrationMessageResult(messageId, true);

        result.MessageId.Should().Be(messageId);
        result.IsValid.Should().BeTrue();
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public void ProcessIntegrationMessageResult_FailureResult_HasFailureReason()
    {
        var messageId = Guid.NewGuid();
        var result = new ProcessIntegrationMessageResult(messageId, false, "Payload is not valid JSON.");

        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be("Payload is not valid JSON.");
    }

    [Fact]
    public void AckPayload_StoresAllProperties()
    {
        var msgId = Guid.NewGuid();
        var corrId = Guid.NewGuid();
        var ack = new AckPayload(msgId, corrId, "Delivered");

        ack.OriginalMessageId.Should().Be(msgId);
        ack.CorrelationId.Should().Be(corrId);
        ack.Outcome.Should().Be("Delivered");
    }

    [Fact]
    public void NackPayload_StoresAllProperties()
    {
        var msgId = Guid.NewGuid();
        var corrId = Guid.NewGuid();
        var nack = new NackPayload(msgId, corrId, "Validation failed");

        nack.OriginalMessageId.Should().Be(msgId);
        nack.CorrelationId.Should().Be(corrId);
        nack.Reason.Should().Be("Validation failed");
    }
}
