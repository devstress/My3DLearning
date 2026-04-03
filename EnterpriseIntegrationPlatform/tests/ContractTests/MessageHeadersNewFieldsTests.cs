using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Contract;

[TestFixture]
public class MessageHeadersNewFieldsTests
{
    [Test]
    public void ReplyTo_HasExpectedValue() =>
        Assert.That(MessageHeaders.ReplyTo, Is.EqualTo("reply-to"));

    [Test]
    public void ExpiresAt_HasExpectedValue() =>
        Assert.That(MessageHeaders.ExpiresAt, Is.EqualTo("expires-at"));

    [Test]
    public void SequenceNumber_HasExpectedValue() =>
        Assert.That(MessageHeaders.SequenceNumber, Is.EqualTo("sequence-number"));

    [Test]
    public void TotalCount_HasExpectedValue() =>
        Assert.That(MessageHeaders.TotalCount, Is.EqualTo("total-count"));

    [Test]
    public void Intent_HasExpectedValue() =>
        Assert.That(MessageHeaders.Intent, Is.EqualTo("intent"));
}
