using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class NatsNotificationActivityServiceTests
{
    private IMessageBrokerProducer _producer = null!;
    private IFeatureFlagService _featureFlags = null!;
    private INotificationMapper _mapper = null!;
    private NatsNotificationActivityService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _featureFlags = Substitute.For<IFeatureFlagService>();
        _mapper = new XmlNotificationMapper();

        // Default: notifications feature flag enabled
        _featureFlags
            .IsEnabledAsync(NotificationFeatureFlags.NotificationsEnabled, null, Arg.Any<CancellationToken>())
            .Returns(true);

        _sut = new NatsNotificationActivityService(
            _producer,
            _featureFlags,
            _mapper,
            NullLogger<NatsNotificationActivityService>.Instance);
    }

    // ── Existing Ack tests ─────────────────────────────────────────────────

    [Test]
    public async Task PublishAckAsync_PublishesToCorrectTopic()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishAckAsync(messageId, correlationId, "integration.ack");

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<NatsNotificationActivityService.AckPayload>>(env =>
                env.MessageType == "Integration.Ack" &&
                env.Source == "Workflow.Temporal" &&
                env.CorrelationId == correlationId),
            "integration.ack",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAckAsync_SetsCorrectPayload()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishAckAsync(messageId, correlationId, "integration.ack");

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<NatsNotificationActivityService.AckPayload>>(env =>
                env.Payload.OriginalMessageId == messageId &&
                env.Payload.CorrelationId == correlationId &&
                env.Payload.Outcome == "Delivered"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAckAsync_SetsCausationIdToMessageId()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishAckAsync(messageId, correlationId, "integration.ack");

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<NatsNotificationActivityService.AckPayload>>(env =>
                env.CausationId == messageId),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Existing Nack tests ────────────────────────────────────────────────

    [Test]
    public async Task PublishNackAsync_PublishesToCorrectTopic()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishNackAsync(messageId, correlationId, "Bad payload", "integration.nack");

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<NatsNotificationActivityService.NackPayload>>(env =>
                env.MessageType == "Integration.Nack" &&
                env.Source == "Workflow.Temporal" &&
                env.CorrelationId == correlationId),
            "integration.nack",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishNackAsync_SetsCorrectPayload()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishNackAsync(messageId, correlationId, "Bad payload", "integration.nack");

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<NatsNotificationActivityService.NackPayload>>(env =>
                env.Payload.OriginalMessageId == messageId &&
                env.Payload.CorrelationId == correlationId &&
                env.Payload.Reason == "Bad payload"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishNackAsync_SetsCausationIdToMessageId()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishNackAsync(messageId, correlationId, "Bad payload", "integration.nack");

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<NatsNotificationActivityService.NackPayload>>(env =>
                env.CausationId == messageId),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Use Case 2: Channel Adapter success → Ack with XML mapped payload ──

    [Test]
    public async Task PublishAckAsync_MapsToXmlAckFormat()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishAckAsync(messageId, correlationId, "integration.ack");

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<NatsNotificationActivityService.AckPayload>>(env =>
                env.Payload.MappedResponse == "<Ack>ok</Ack>"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Use Case 3: Channel Adapter timeout → Nack with XML mapped payload ──

    [Test]
    public async Task PublishNackAsync_MapsToXmlNackFormat_WithErrorMessage()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishNackAsync(
            messageId, correlationId, "Connection timed out", "integration.nack");

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<NatsNotificationActivityService.NackPayload>>(env =>
                env.Payload.MappedResponse == "<Nack>not ok because of Connection timed out</Nack>"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Use Case 4: Feature flag OFF → Ack skipped; ON → Ack resumes ───────

    [Test]
    public async Task PublishAckAsync_FeatureFlagDisabled_DoesNotPublish()
    {
        _featureFlags
            .IsEnabledAsync(NotificationFeatureFlags.NotificationsEnabled, null, Arg.Any<CancellationToken>())
            .Returns(false);

        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishAckAsync(messageId, correlationId, "integration.ack");

        await _producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<NatsNotificationActivityService.AckPayload>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAckAsync_FeatureFlagReEnabled_ResumesPublishing()
    {
        // First call: disabled
        _featureFlags
            .IsEnabledAsync(NotificationFeatureFlags.NotificationsEnabled, null, Arg.Any<CancellationToken>())
            .Returns(false, true);

        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        // Call 1: flag disabled → no publish
        await _sut.PublishAckAsync(messageId, correlationId, "integration.ack");
        await _producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<NatsNotificationActivityService.AckPayload>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Call 2: flag re-enabled → publishes
        await _sut.PublishAckAsync(messageId, correlationId, "integration.ack");
        await _producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<NatsNotificationActivityService.AckPayload>>(),
            "integration.ack",
            Arg.Any<CancellationToken>());
    }

    // ── Use Case 5: Feature flag OFF → Nack skipped; ON → Nack resumes ─────

    [Test]
    public async Task PublishNackAsync_FeatureFlagDisabled_DoesNotPublish()
    {
        _featureFlags
            .IsEnabledAsync(NotificationFeatureFlags.NotificationsEnabled, null, Arg.Any<CancellationToken>())
            .Returns(false);

        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishNackAsync(messageId, correlationId, "Timeout", "integration.nack");

        await _producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<NatsNotificationActivityService.NackPayload>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishNackAsync_FeatureFlagReEnabled_ResumesPublishing()
    {
        _featureFlags
            .IsEnabledAsync(NotificationFeatureFlags.NotificationsEnabled, null, Arg.Any<CancellationToken>())
            .Returns(false, true);

        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        // Call 1: flag disabled → no publish
        await _sut.PublishNackAsync(messageId, correlationId, "Timeout", "integration.nack");
        await _producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<NatsNotificationActivityService.NackPayload>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Call 2: flag re-enabled → publishes
        await _sut.PublishNackAsync(messageId, correlationId, "Timeout", "integration.nack");
        await _producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<NatsNotificationActivityService.NackPayload>>(),
            "integration.nack",
            Arg.Any<CancellationToken>());
    }
}
