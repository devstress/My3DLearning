using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Recipient List pattern.
/// Routes a single message to multiple dynamic destinations.
/// BizTalk equivalent: Dynamic Send Port Groups with expression-based routing.
/// EIP: Recipient List (p. 249)
/// </summary>
public class RecipientListTests
{
    private record InvoicePayload(string CustomerId, decimal Total, bool IsInternational);

    [Fact]
    public void Routes_InternationalInvoice_ToMultipleRecipients()
    {
        var list = new RecipientList<InvoicePayload>()
            .AddRecipient(_ => true, "accounting-queue") // always
            .AddRecipient(e => e.Payload.Total > 5000, "audit-queue")
            .AddRecipient(e => e.Payload.IsInternational, "customs-queue");

        var envelope = IntegrationEnvelope<InvoicePayload>.Create(
            new InvoicePayload("CUST-42", 15_000, true), "Billing", "InvoiceCreated");

        var recipients = list.DetermineRecipients(envelope);

        recipients.Should().HaveCount(3);
        recipients.Should().Contain("accounting-queue");
        recipients.Should().Contain("audit-queue");
        recipients.Should().Contain("customs-queue");
    }

    [Fact]
    public void Routes_DomesticSmallInvoice_ToAccountingOnly()
    {
        var list = new RecipientList<InvoicePayload>()
            .AddRecipient(_ => true, "accounting-queue")
            .AddRecipient(e => e.Payload.Total > 5000, "audit-queue")
            .AddRecipient(e => e.Payload.IsInternational, "customs-queue");

        var envelope = IntegrationEnvelope<InvoicePayload>.Create(
            new InvoicePayload("CUST-01", 100, false), "Billing", "InvoiceCreated");

        var recipients = list.DetermineRecipients(envelope);

        recipients.Should().ContainSingle().Which.Should().Be("accounting-queue");
    }
}
