using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Message Translator pattern.
/// Converts a message from one format to another.
/// BizTalk equivalent: BizTalk Map (XSLT transforms, Functoids).
/// EIP: Message Translator (p. 85)
/// </summary>
public class MessageTranslatorTests
{
    private record ExternalOrder(string OrderNumber, string CustomerName, double Total);
    private record InternalOrder(string Id, string Customer, decimal Amount);

    [Fact]
    public void Translates_ExternalFormat_ToInternalFormat()
    {
        var translator = new MessageTranslator<ExternalOrder, InternalOrder>(
            external => new InternalOrder(
                external.OrderNumber,
                external.CustomerName,
                (decimal)external.Total),
            outputMessageType: "InternalOrderCreated");

        var input = IntegrationEnvelope<ExternalOrder>.Create(
            new ExternalOrder("ORD-123", "Acme Corp", 1500.50),
            "ExternalERP", "ExternalOrderCreated");

        var result = translator.Transform(input);

        result.Payload.Id.Should().Be("ORD-123");
        result.Payload.Customer.Should().Be("Acme Corp");
        result.Payload.Amount.Should().Be(1500.50m);
        result.MessageType.Should().Be("InternalOrderCreated");
        result.CorrelationId.Should().Be(input.CorrelationId);
    }
}
