using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Dead Letter Channel pattern.
/// Handles messages that cannot be delivered or processed by routing them
/// to a dedicated fault/dead-letter destination.
/// BizTalk equivalent: Failed Message Routing, Suspended queue.
/// EIP: Dead Letter Channel (p. 119)
/// </summary>
public class DeadLetterChannelTests
{
    private record PaymentPayload(string PaymentId, decimal Amount);

    [Fact]
    public void Creates_FaultEnvelope_FromFailedMessage()
    {
        var original = IntegrationEnvelope<PaymentPayload>.Create(
            new PaymentPayload("PAY-001", 500),
            "PaymentGateway", "PaymentReceived");

        var exception = new InvalidOperationException("Insufficient funds");

        var fault = FaultEnvelope.Create(original, "PaymentProcessor", "Payment processing failed", retryCount: 3, exception);

        fault.OriginalMessageId.Should().Be(original.MessageId);
        fault.CorrelationId.Should().Be(original.CorrelationId);
        fault.FaultReason.Should().Be("Payment processing failed");
        fault.ErrorDetails.Should().Contain("Insufficient funds");
        fault.FaultId.Should().NotBeEmpty();
        fault.RetryCount.Should().Be(3);
    }

    [Fact]
    public void DeliveryStatus_HasDeadLettered_State()
    {
        // The platform defines explicit delivery states including Dead Letter
        DeliveryStatus.DeadLettered.Should().BeDefined();
        DeliveryStatus.Failed.Should().BeDefined();
        DeliveryStatus.Retrying.Should().BeDefined();
    }

    [Fact]
    public void FaultEnvelope_CapturesExceptionDetails()
    {
        var payload = new PaymentPayload("PAY-002", 100);
        var envelope = IntegrationEnvelope<PaymentPayload>.Create(
            payload, "Gateway", "Payment");

        try
        {
            throw new ArgumentException("Invalid card number", "cardNumber");
        }
        catch (Exception ex)
        {
            var fault = FaultEnvelope.Create(envelope, "Validator", "Validation failed", retryCount: 0, ex);

            fault.ErrorDetails.Should().Contain("ArgumentException");
            fault.ErrorDetails.Should().Contain("Invalid card number");
        }
    }
}
