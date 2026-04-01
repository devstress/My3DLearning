using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Contract;

[TestFixture]
public class MessageHeadersTests
{
    [Test]
    public void TraceId_HasExpectedValue() =>
        Assert.That(MessageHeaders.TraceId, Is.EqualTo("trace-id"));

    [Test]
    public void SpanId_HasExpectedValue() =>
        Assert.That(MessageHeaders.SpanId, Is.EqualTo("span-id"));

    [Test]
    public void ContentType_HasExpectedValue() =>
        Assert.That(MessageHeaders.ContentType, Is.EqualTo("content-type"));

    [Test]
    public void SchemaVersion_HasExpectedValue() =>
        Assert.That(MessageHeaders.SchemaVersion, Is.EqualTo("schema-version"));

    [Test]
    public void RetryCount_HasExpectedValue() =>
        Assert.That(MessageHeaders.RetryCount, Is.EqualTo("retry-count"));

    [Test]
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

        Assert.That(constants, Has.All.Not.Null.And.All.Not.Empty);
    }
}
