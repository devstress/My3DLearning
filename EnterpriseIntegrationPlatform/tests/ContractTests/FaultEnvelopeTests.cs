using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Contract;

[TestFixture]
public class FaultEnvelopeTests
{
    private static IntegrationEnvelope<string> BuildEnvelope() =>
        IntegrationEnvelope<string>.Create("test-payload", "test-service", "TestEvent");

    // ── Create factory ────────────────────────────────────────────────────────

    [Test]
    public void Create_SetsNewFaultId()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "Unhandled error", retryCount: 3);

        Assert.That(fault.FaultId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Create_CopiesOriginalMessageId()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "Unhandled error", retryCount: 0);

        Assert.That(fault.OriginalMessageId, Is.EqualTo(original.MessageId));
    }

    [Test]
    public void Create_CopiesCorrelationId()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "Unhandled error", retryCount: 0);

        Assert.That(fault.CorrelationId, Is.EqualTo(original.CorrelationId));
    }

    [Test]
    public void Create_CopiesMessageType()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "Bad format", retryCount: 1);

        Assert.That(fault.OriginalMessageType, Is.EqualTo(original.MessageType));
    }

    [Test]
    public void Create_SetsRetryCount()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "reason", retryCount: 5);

        Assert.That(fault.RetryCount, Is.EqualTo(5));
    }

    [Test]
    public void Create_SetsNullErrorDetails_WhenNoException()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "processor", "reason", retryCount: 0);

        Assert.That(fault.ErrorDetails, Is.Null);
    }

    [Test]
    public void Create_SetsErrorDetails_WhenExceptionProvided()
    {
        var original = BuildEnvelope();
        var exception = new InvalidOperationException("Something went wrong");

        var fault = FaultEnvelope.Create(original, "processor", "reason", retryCount: 2, exception);

        Assert.That(fault.ErrorDetails, Does.Contain("InvalidOperationException"));
        Assert.That(fault.ErrorDetails, Does.Contain("Something went wrong"));
    }

    [Test]
    public void Create_SetsFaultedAtToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var original = BuildEnvelope();
        var fault = FaultEnvelope.Create(original, "processor", "reason", retryCount: 0);
        var after = DateTimeOffset.UtcNow;

        Assert.That(fault.FaultedAt, Is.GreaterThanOrEqualTo(before).And.LessThanOrEqualTo(after));
    }

    [Test]
    public void Create_SetsFaultedBy()
    {
        var original = BuildEnvelope();

        var fault = FaultEnvelope.Create(original, "ingestion-kafka", "reason", retryCount: 0);

        Assert.That(fault.FaultedBy, Is.EqualTo("ingestion-kafka"));
    }
}
