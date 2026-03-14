using EnterpriseIntegrationPlatform.Contracts;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Contract;

public class IntegrationEnvelopeTests
{
    // ── Create factory ────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsNewMessageId()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        envelope.MessageId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_GeneratesNewCorrelationIdWhenNotSupplied()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        envelope.CorrelationId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_UsesProvidedCorrelationId()
    {
        var correlationId = Guid.NewGuid();

        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent", correlationId: correlationId);

        envelope.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void Create_SetsCausationId_WhenProvided()
    {
        var causationId = Guid.NewGuid();

        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent", causationId: causationId);

        envelope.CausationId.Should().Be(causationId);
    }

    [Fact]
    public void Create_LeavesNullCausationId_WhenNotProvided()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        envelope.CausationId.Should().BeNull();
    }

    [Fact]
    public void Create_SetsTimestampToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");
        var after = DateTimeOffset.UtcNow;

        envelope.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_SetsSourceAndMessageType()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "gateway-api", "OrderCreated");

        envelope.Source.Should().Be("gateway-api");
        envelope.MessageType.Should().Be("OrderCreated");
    }

    [Fact]
    public void Create_DefaultsSchemaVersionToOnePointZero()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        envelope.SchemaVersion.Should().Be("1.0");
    }

    [Fact]
    public void Create_DefaultsPriorityToNormal()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        envelope.Priority.Should().Be(MessagePriority.Normal);
    }

    [Fact]
    public void Create_SetsPayload()
    {
        var envelope = IntegrationEnvelope<int>.Create(42, "svc", "TestEvent");

        envelope.Payload.Should().Be(42);
    }

    [Fact]
    public void Create_InitialisesEmptyMetadata()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        envelope.Metadata.Should().BeEmpty();
    }

    // ── Two calls produce different MessageIds ────────────────────────────────

    [Fact]
    public void Create_TwoCallsProduce_DifferentMessageIds()
    {
        var first = IntegrationEnvelope<string>.Create("x", "svc", "Ev");
        var second = IntegrationEnvelope<string>.Create("x", "svc", "Ev");

        first.MessageId.Should().NotBe(second.MessageId);
    }

    // ── Record equality ───────────────────────────────────────────────────────

    [Fact]
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

        a.Should().Be(b);
    }
}
