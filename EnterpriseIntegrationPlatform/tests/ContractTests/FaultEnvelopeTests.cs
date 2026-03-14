using EnterpriseIntegrationPlatform.Contracts;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Contract;

public class FaultEnvelopeTests
{
    private static IntegrationEnvelope<string> BuildEnvelope() =>
        IntegrationEnvelope<string>.Create("test-payload", "test-service", "TestEvent");

    // ── Create factory ────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsNewFaultId()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "Unhandled error", retryCount: 3);

        fault.FaultId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_CopiesOriginalMessageId()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "Unhandled error", retryCount: 0);

        fault.OriginalMessageId.Should().Be(original.MessageId);
    }

    [Fact]
    public void Create_CopiesCorrelationId()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "Unhandled error", retryCount: 0);

        fault.CorrelationId.Should().Be(original.CorrelationId);
    }

    [Fact]
    public void Create_CopiesMessageType()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "Bad format", retryCount: 1);

        fault.OriginalMessageType.Should().Be(original.MessageType);
    }

    [Fact]
    public void Create_SetsRetryCount()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "reason", retryCount: 5);

        fault.RetryCount.Should().Be(5);
    }

    [Fact]
    public void Create_SetsNullErrorDetails_WhenNoException()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "reason", retryCount: 0);

        fault.ErrorDetails.Should().BeNull();
    }

    [Fact]
    public void Create_SetsErrorDetails_WhenExceptionProvided()
    {
        var original = BuildEnvelope();
        var exception = new InvalidOperationException("Something went wrong");

        var fault = FaultEnvelope.Create(original, "processor", "reason", retryCount: 2, exception);

        fault.ErrorDetails.Should().Contain("InvalidOperationException");
        fault.ErrorDetails.Should().Contain("Something went wrong");
    }

    [Fact]
    public void Create_SetsFaultedAtToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var original = BuildEnvelope();
        var fault = FaultEnvelope.Create(original, "processor", "reason", retryCount: 0);
        var after = DateTimeOffset.UtcNow;

        fault.FaultedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_SetsFaultedBy()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "ingestion-kafka", "reason", retryCount: 0);

        fault.FaultedBy.Should().Be("ingestion-kafka");
    }
}
