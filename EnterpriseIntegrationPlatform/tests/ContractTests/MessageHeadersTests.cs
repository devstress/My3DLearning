using EnterpriseIntegrationPlatform.Contracts;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Contract;

public class MessageHeadersTests
{
    [Fact]
    public void TraceId_HasExpectedValue() =>
        MessageHeaders.TraceId.Should().Be("trace-id");

    [Fact]
    public void SpanId_HasExpectedValue() =>
        MessageHeaders.SpanId.Should().Be("span-id");

    [Fact]
    public void ContentType_HasExpectedValue() =>
        MessageHeaders.ContentType.Should().Be("content-type");

    [Fact]
    public void SchemaVersion_HasExpectedValue() =>
        MessageHeaders.SchemaVersion.Should().Be("schema-version");

    [Fact]
    public void RetryCount_HasExpectedValue() =>
        MessageHeaders.RetryCount.Should().Be("retry-count");

    [Fact]
    public void AllConstantsAreNonEmpty()
    {
        var constants = new[]
        {
            MessageHeaders.TraceId,
            MessageHeaders.SpanId,
            MessageHeaders.ContentType,
            MessageHeaders.SchemaVersion,
            MessageHeaders.SourceTopic,
            MessageHeaders.ConsumerGroup,
            MessageHeaders.LastAttemptAt,
            MessageHeaders.RetryCount,
        };

        constants.Should().AllSatisfy(c => c.Should().NotBeNullOrWhiteSpace());
    }
}
