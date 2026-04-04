using EnterpriseIntegrationPlatform.Activities;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class IntegrationPipelineInputNotificationTests
{
    // ── Use Case 1: Default (no notifications) → backward compatible ──────

    [Test]
    public void NotificationsEnabled_DefaultsToFalse()
    {
        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            Timestamp: DateTimeOffset.UtcNow,
            Source: "test-system",
            MessageType: "TestMessage",
            SchemaVersion: "1.0",
            Priority: 1,
            PayloadJson: "{}",
            MetadataJson: null,
            AckSubject: "integration.ack",
            NackSubject: "integration.nack");

        Assert.That(input.NotificationsEnabled, Is.False,
            "Existing integrations without explicit notification opt-in must not send Ack/Nack");
    }

    // ── Use Case 2 & 3: Explicitly enabled → notifications sent ───────────

    [Test]
    public void NotificationsEnabled_WhenTrue_EnablesNotifications()
    {
        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            Timestamp: DateTimeOffset.UtcNow,
            Source: "test-system",
            MessageType: "TestMessage",
            SchemaVersion: "1.0",
            Priority: 1,
            PayloadJson: "{}",
            MetadataJson: null,
            AckSubject: "integration.ack",
            NackSubject: "integration.nack",
            NotificationsEnabled: true);

        Assert.That(input.NotificationsEnabled, Is.True,
            "Integration with explicit opt-in should have notifications enabled");
    }

    [Test]
    public void NotificationsEnabled_PreservesValueOnRecordWith()
    {
        var original = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            Timestamp: DateTimeOffset.UtcNow,
            Source: "test-system",
            MessageType: "TestMessage",
            SchemaVersion: "1.0",
            Priority: 1,
            PayloadJson: "{}",
            MetadataJson: null,
            AckSubject: "integration.ack",
            NackSubject: "integration.nack",
            NotificationsEnabled: false);

        var withNotifications = original with { NotificationsEnabled = true };

        Assert.That(original.NotificationsEnabled, Is.False);
        Assert.That(withNotifications.NotificationsEnabled, Is.True);
    }
}
