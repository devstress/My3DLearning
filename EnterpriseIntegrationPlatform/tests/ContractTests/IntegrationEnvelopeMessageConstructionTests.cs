using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Contract;

[TestFixture]
public class IntegrationEnvelopeMessageConstructionTests
{
    // ── ReplyTo (Return Address) ──────────────────────────────────────────────

    [Test]
    public void Create_DefaultsReplyToToNull()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.ReplyTo, Is.Null);
    }

    [Test]
    public void ReplyTo_CanBeSetViaInitialiser()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ReplyTo = "replies.orders" };

        Assert.That(envelope.ReplyTo, Is.EqualTo("replies.orders"));
    }

    [Test]
    public void ReplyTo_SurvivesJsonRoundTrip()
    {
        var original = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ReplyTo = "replies.orders" };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<IntegrationEnvelope<string>>(json);

        Assert.That(deserialized!.ReplyTo, Is.EqualTo("replies.orders"));
    }

    // ── ExpiresAt (Message Expiration) ────────────────────────────────────────

    [Test]
    public void Create_DefaultsExpiresAtToNull()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.ExpiresAt, Is.Null);
    }

    [Test]
    public void ExpiresAt_CanBeSetViaInitialiser()
    {
        var expiry = DateTimeOffset.UtcNow.AddHours(1);

        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ExpiresAt = expiry };

        Assert.That(envelope.ExpiresAt, Is.EqualTo(expiry));
    }

    [Test]
    public void ExpiresAt_SurvivesJsonRoundTrip()
    {
        var expiry = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        var original = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ExpiresAt = expiry };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<IntegrationEnvelope<string>>(json);

        Assert.That(deserialized!.ExpiresAt, Is.EqualTo(expiry));
    }

    [Test]
    public void IsExpired_ReturnsFalse_WhenExpiresAtIsNull()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.IsExpired, Is.False);
    }

    [Test]
    public void IsExpired_ReturnsFalse_WhenExpiresAtIsInFuture()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };

        Assert.That(envelope.IsExpired, Is.False);
    }

    [Test]
    public void IsExpired_ReturnsTrue_WhenExpiresAtIsInPast()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1) };

        Assert.That(envelope.IsExpired, Is.True);
    }

    // ── SequenceNumber / TotalCount (Message Sequence) ────────────────────────

    [Test]
    public void Create_DefaultsSequenceNumberToNull()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.SequenceNumber, Is.Null);
    }

    [Test]
    public void Create_DefaultsTotalCountToNull()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.TotalCount, Is.Null);
    }

    [Test]
    public void SequenceNumber_CanBeSetViaInitialiser()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { SequenceNumber = 2, TotalCount = 5 };

        Assert.That(envelope.SequenceNumber, Is.EqualTo(2));
        Assert.That(envelope.TotalCount, Is.EqualTo(5));
    }

    [Test]
    public void SequenceNumber_SurvivesJsonRoundTrip()
    {
        var original = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { SequenceNumber = 3, TotalCount = 10 };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<IntegrationEnvelope<string>>(json);

        Assert.That(deserialized!.SequenceNumber, Is.EqualTo(3));
        Assert.That(deserialized.TotalCount, Is.EqualTo(10));
    }

    // ── Intent (Command / Document / Event Messages) ──────────────────────────

    [Test]
    public void Create_DefaultsIntentToNull()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        Assert.That(envelope.Intent, Is.Null);
    }

    [Test]
    public void Intent_CanBeSetToCommand()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "PlaceOrder")
            with { Intent = MessageIntent.Command };

        Assert.That(envelope.Intent, Is.EqualTo(MessageIntent.Command));
    }

    [Test]
    public void Intent_CanBeSetToDocument()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "Invoice")
            with { Intent = MessageIntent.Document };

        Assert.That(envelope.Intent, Is.EqualTo(MessageIntent.Document));
    }

    [Test]
    public void Intent_CanBeSetToEvent()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "OrderCreated")
            with { Intent = MessageIntent.Event };

        Assert.That(envelope.Intent, Is.EqualTo(MessageIntent.Event));
    }

    [Test]
    public void Intent_SurvivesJsonRoundTrip()
    {
        var original = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { Intent = MessageIntent.Event };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<IntegrationEnvelope<string>>(json);

        Assert.That(deserialized!.Intent, Is.EqualTo(MessageIntent.Event));
    }

    // ── All new fields in combination ─────────────────────────────────────────

    [Test]
    public void AllNewFields_SurviveJsonRoundTrip()
    {
        var expiry = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);

        var original = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent") with
        {
            ReplyTo = "replies.topic",
            ExpiresAt = expiry,
            SequenceNumber = 0,
            TotalCount = 3,
            Intent = MessageIntent.Command,
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<IntegrationEnvelope<string>>(json);

        Assert.That(deserialized!.ReplyTo, Is.EqualTo("replies.topic"));
        Assert.That(deserialized.ExpiresAt, Is.EqualTo(expiry));
        Assert.That(deserialized.SequenceNumber, Is.EqualTo(0));
        Assert.That(deserialized.TotalCount, Is.EqualTo(3));
        Assert.That(deserialized.Intent, Is.EqualTo(MessageIntent.Command));
    }

    [Test]
    public void Envelope_WithSameNewFields_AreEqual()
    {
        var id = Guid.NewGuid();
        var corr = Guid.NewGuid();
        var ts = DateTimeOffset.UtcNow;
        var expiry = DateTimeOffset.UtcNow.AddHours(1);

        var a = new IntegrationEnvelope<string>
        {
            MessageId = id,
            CorrelationId = corr,
            Timestamp = ts,
            Source = "svc",
            MessageType = "Event",
            Payload = "hello",
            ReplyTo = "replies",
            ExpiresAt = expiry,
            SequenceNumber = 1,
            TotalCount = 5,
            Intent = MessageIntent.Document,
        };

        var b = a with { };

        Assert.That(a, Is.EqualTo(b));
    }
}
