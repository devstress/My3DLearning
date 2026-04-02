using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Contract;

[TestFixture]
public class IntegrationEnvelopeTests
{
    // ── Create factory ────────────────────────────────────────────────────────

    [Test]
    public void Create_SetsNewMessageId()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Create_GeneratesNewCorrelationIdWhenNotSupplied()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.CorrelationId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Create_UsesProvidedCorrelationId()
    {
        var correlationId = Guid.NewGuid();

        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent", correlationId: correlationId);

        Assert.That(envelope.CorrelationId, Is.EqualTo(correlationId));
    }

    [Test]
    public void Create_SetsCausationId_WhenProvided()
    {
        var causationId = Guid.NewGuid();

        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent", causationId: causationId);

        Assert.That(envelope.CausationId, Is.EqualTo(causationId));
    }

    [Test]
    public void Create_LeavesNullCausationId_WhenNotProvided()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.CausationId, Is.Null);
    }

    [Test]
    public void Create_SetsTimestampToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");
        var after = DateTimeOffset.UtcNow;

        Assert.That(envelope.Timestamp, Is.GreaterThanOrEqualTo(before).And.LessThanOrEqualTo(after));
    }

    [Test]
    public void Create_SetsSourceAndMessageType()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "gateway-api", "OrderCreated");

        Assert.That(envelope.Source, Is.EqualTo("gateway-api"));
        Assert.That(envelope.MessageType, Is.EqualTo("OrderCreated"));
    }

    [Test]
    public void Create_DefaultsSchemaVersionToOnePointZero()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.SchemaVersion, Is.EqualTo("1.0"));
    }

    [Test]
    public void Create_DefaultsPriorityToNormal()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.Priority, Is.EqualTo(MessagePriority.Normal));
    }

    [Test]
    public void Create_SetsPayload()
    {
        var envelope = IntegrationEnvelope<int>.Create(42, "svc", "TestEvent");

        Assert.That(envelope.Payload, Is.EqualTo(42));
    }

    [Test]
    public void Create_InitialisesEmptyMetadata()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.Metadata, Is.Empty);
    }

    // ── Two calls produce different MessageIds ────────────────────────────────

    [Test]
    public void Create_TwoCallsProduce_DifferentMessageIds()
    {
        var first = IntegrationEnvelope<string>.Create("x", "svc", "Ev");
        var second = IntegrationEnvelope<string>.Create("x", "svc", "Ev");

        Assert.That(first.MessageId, Is.Not.EqualTo(second.MessageId));
    }

    // ── Record equality ───────────────────────────────────────────────────────

    [Test]
    public void Envelope_WithSameValues_AreEqual()
    {
        var id = Guid.NewGuid();
        var corr = Guid.NewGuid();
        var ts = DateTimeOffset.UtcNow;

        var a = new IntegrationEnvelope<string>
        {
            MessageId = id,
            CorrelationId = corr,
            Timestamp = ts,
            Source = "svc",
            MessageType = "Event",
            Payload = "hello",
        };

        var b = a with { };

        Assert.That(a, Is.EqualTo(b));
    }
}
