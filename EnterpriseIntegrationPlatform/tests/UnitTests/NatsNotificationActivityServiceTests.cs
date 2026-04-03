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
    private NatsNotificationActivityService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _sut = new NatsNotificationActivityService(
            _producer,
            NullLogger<NatsNotificationActivityService>.Instance);
    }

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
}
