using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Content Filter pattern.
/// Removes or normalizes fields from a message, producing a simpler output.
/// BizTalk equivalent: Map that strips fields, pipeline component for data masking.
/// EIP: Content Filter (p. 342)
/// </summary>
public class ContentFilterTests
{
    private record FullCustomerRecord(string Id, string Name, string Ssn, string Email, string Phone);
    private record PublicCustomerRecord(string Id, string Name, string Email);

    [Fact]
    public void Filters_SensitiveData_FromMessage()
    {
        var filter = new ContentFilter<FullCustomerRecord, PublicCustomerRecord>(
            full => new PublicCustomerRecord(full.Id, full.Name, full.Email));

        var input = IntegrationEnvelope<FullCustomerRecord>.Create(
            new FullCustomerRecord("C-01", "Jane Doe", "123-45-6789", "jane@example.com", "+1-555-0100"),
            "CRM", "CustomerExport");

        var result = filter.Filter(input);

        result.Payload.Id.Should().Be("C-01");
        result.Payload.Name.Should().Be("Jane Doe");
        result.Payload.Email.Should().Be("jane@example.com");
        result.CorrelationId.Should().Be(input.CorrelationId);
    }
}
