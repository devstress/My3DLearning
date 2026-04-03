using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class CassandraPersistenceActivityServiceTests
{
    private IMessageRepository _repository = null!;
    private CassandraPersistenceActivityService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IMessageRepository>();
        _sut = new CassandraPersistenceActivityService(
            _repository,
            NullLogger<CassandraPersistenceActivityService>.Instance);
    }

    private static IntegrationPipelineInput BuildInput() =>
        new(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: Guid.NewGuid(),
            Timestamp: DateTimeOffset.UtcNow,
            Source: "TestSource",
            MessageType: "OrderCreated",
            SchemaVersion: "1.0",
            Priority: (int)MessagePriority.Normal,
            PayloadJson: """{"orderId":1}""",
            MetadataJson: null,
            AckSubject: "integration.ack",
            NackSubject: "integration.nack");

    [Test]
    public async Task SaveMessageAsync_PersistsToRepository()
    {
        var input = BuildInput();

        await _sut.SaveMessageAsync(input);

        await _repository.Received(1).SaveMessageAsync(
            Arg.Is<MessageRecord>(r =>
                r.MessageId == input.MessageId &&
                r.CorrelationId == input.CorrelationId &&
                r.Source == input.Source &&
                r.MessageType == input.MessageType &&
                r.PayloadJson == input.PayloadJson &&
                r.DeliveryStatus == DeliveryStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveMessageAsync_SetsCausationId()
    {
        var input = BuildInput();

        await _sut.SaveMessageAsync(input);

        await _repository.Received(1).SaveMessageAsync(
            Arg.Is<MessageRecord>(r => r.CausationId == input.CausationId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveMessageAsync_MapsPriorityCorrectly()
    {
        var input = BuildInput();

        await _sut.SaveMessageAsync(input);

        await _repository.Received(1).SaveMessageAsync(
            Arg.Is<MessageRecord>(r => r.Priority == (MessagePriority)input.Priority),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateDeliveryStatusAsync_UpdatesWithDeliveredStatus()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var recordedAt = DateTimeOffset.UtcNow;

        await _sut.UpdateDeliveryStatusAsync(messageId, correlationId, recordedAt, "Delivered");

        await _repository.Received(1).UpdateDeliveryStatusAsync(
            messageId, correlationId, recordedAt, DeliveryStatus.Delivered,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateDeliveryStatusAsync_UpdatesWithFailedStatus()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var recordedAt = DateTimeOffset.UtcNow;

        await _sut.UpdateDeliveryStatusAsync(messageId, correlationId, recordedAt, "Failed");

        await _repository.Received(1).UpdateDeliveryStatusAsync(
            messageId, correlationId, recordedAt, DeliveryStatus.Failed,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveFaultAsync_PersistsFaultToRepository()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.SaveFaultAsync(messageId, correlationId, "OrderCreated", "Workflow.Temporal", "Bad payload", 2);

        await _repository.Received(1).SaveFaultAsync(
            Arg.Is<FaultEnvelope>(f =>
                f.OriginalMessageId == messageId &&
                f.CorrelationId == correlationId &&
                f.OriginalMessageType == "OrderCreated" &&
                f.FaultedBy == "Workflow.Temporal" &&
                f.FaultReason == "Bad payload" &&
                f.RetryCount == 2),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveFaultAsync_GeneratesUniqueFaultId()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.SaveFaultAsync(messageId, correlationId, "OrderCreated", "svc", "fail", 0);

        await _repository.Received(1).SaveFaultAsync(
            Arg.Is<FaultEnvelope>(f => f.FaultId != Guid.Empty),
            Arg.Any<CancellationToken>());
    }
}
