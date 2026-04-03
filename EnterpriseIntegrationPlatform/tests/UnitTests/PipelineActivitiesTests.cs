using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PipelineActivitiesTests
{
    private IPersistenceActivityService _persistence = null!;
    private INotificationActivityService _notification = null!;
    private IMessageLoggingService _logging = null!;
    private PipelineActivities _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IPersistenceActivityService>();
        _notification = Substitute.For<INotificationActivityService>();
        _logging = Substitute.For<IMessageLoggingService>();
        _sut = new PipelineActivities(_persistence, _notification, _logging);
    }

    private static IntegrationPipelineInput BuildInput(
        string payloadJson = """{"orderId":1}""") =>
        new(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            Timestamp: DateTimeOffset.UtcNow,
            Source: "TestSource",
            MessageType: "OrderCreated",
            SchemaVersion: "1.0",
            Priority: 1,
            PayloadJson: payloadJson,
            MetadataJson: null,
            AckSubject: "integration.ack",
            NackSubject: "integration.nack");

    [Test]
    public async Task PersistMessageAsync_DelegatesToPersistenceService()
    {
        var input = BuildInput();

        await _sut.PersistMessageAsync(input);

        await _persistence.Received(1).SaveMessageAsync(input, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateDeliveryStatusAsync_DelegatesToPersistenceService()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var recordedAt = DateTimeOffset.UtcNow;

        await _sut.UpdateDeliveryStatusAsync(messageId, correlationId, recordedAt, "Delivered");

        await _persistence.Received(1).UpdateDeliveryStatusAsync(
            messageId, correlationId, recordedAt, "Delivered", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveFaultAsync_DelegatesToPersistenceService()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.SaveFaultAsync(messageId, correlationId, "OrderCreated", "Workflow.Temporal", "Bad payload", 0);

        await _persistence.Received(1).SaveFaultAsync(
            messageId, correlationId, "OrderCreated", "Workflow.Temporal", "Bad payload", 0,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAckAsync_DelegatesToNotificationService()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishAckAsync(messageId, correlationId, "integration.ack");

        await _notification.Received(1).PublishAckAsync(
            messageId, correlationId, "integration.ack", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishNackAsync_DelegatesToNotificationService()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await _sut.PublishNackAsync(messageId, correlationId, "Bad payload", "integration.nack");

        await _notification.Received(1).PublishNackAsync(
            messageId, correlationId, "Bad payload", "integration.nack", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LogStageAsync_DelegatesToLoggingService()
    {
        var messageId = Guid.NewGuid();

        await _sut.LogStageAsync(messageId, "OrderCreated", "Received");

        await _logging.Received(1).LogAsync(messageId, "OrderCreated", "Received");
    }
}
