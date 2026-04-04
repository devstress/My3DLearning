using NSubstitute;
using NUnit.Framework;

using Temporalio.Client;
using Temporalio.Testing;
using Temporalio.Worker;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Workflows;

namespace EnterpriseIntegrationPlatform.Tests.Workflow;

/// <summary>
/// Comprehensive integration tests for the <see cref="AtomicPipelineWorkflow"/>.
/// Verifies end-to-end atomic semantics: success → Ack, failure → compensate all
/// previously ack'd steps → Nack. Uses Temporal's local dev server for real
/// workflow execution (not mocked).
/// </summary>
[TestFixture]
public class AtomicPipelineWorkflowTests
{
    private WorkflowEnvironment? _env;
    private bool _serverAvailable;

    [SetUp]
    public async Task SetUp()
    {
        try
        {
            _env = await WorkflowEnvironment.StartLocalAsync();
            _serverAvailable = true;
        }
        catch (Exception)
        {
            _serverAvailable = false;
        }
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_env is not null)
        {
            await _env.DisposeAsync();
        }
    }

    private bool SkipIfNoServer() => !_serverAvailable;

    private static IntegrationPipelineInput BuildInput(
        string payloadJson = """{"orderId":42}""") =>
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

    // ─────────────────────────────────────────────────────────────────────────
    // SUCCESS PATH — Full pipeline → Ack
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task RunAsync_ValidMessage_ReturnsSuccess_PublishesAck()
    {
        if (SkipIfNoServer()) return;

        var validation = Substitute.For<IMessageValidationService>();
        validation.ValidateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(MessageValidationResult.Success);

        var logging = Substitute.For<IMessageLoggingService>();
        var persistence = Substitute.For<IPersistenceActivityService>();
        var notification = Substitute.For<INotificationActivityService>();
        var compensation = Substitute.For<ICompensationActivityService>();

        var integrationActivities = new IntegrationActivities(validation, logging);
        var pipelineActivities = new PipelineActivities(persistence, notification, logging);
        var sagaActivities = new SagaCompensationActivities(compensation, logging);

        var input = BuildInput();

        using var worker = new TemporalWorker(
            _env!.Client,
            new TemporalWorkerOptions("test-atomic-queue")
                .AddWorkflow<AtomicPipelineWorkflow>()
                .AddAllActivities(integrationActivities)
                .AddAllActivities(pipelineActivities)
                .AddAllActivities(sagaActivities));

        var result = await worker.ExecuteAsync(() =>
            _env.Client.ExecuteWorkflowAsync(
                (AtomicPipelineWorkflow wf) => wf.RunAsync(input),
                new(id: $"test-atomic-success-{Guid.NewGuid()}", taskQueue: "test-atomic-queue")));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.MessageId, Is.EqualTo(input.MessageId));
        Assert.That(result.FailureReason, Is.Null);
        Assert.That(result.CompensatedSteps, Is.Null);

        // Verify Ack was published
        await notification.Received(1).PublishAckAsync(
            input.MessageId, input.CorrelationId, "integration.ack", Arg.Any<CancellationToken>());

        // Verify no compensation occurred
        await compensation.DidNotReceiveWithAnyArgs().CompensateAsync(default, default!);

        // Verify no Nack was published
        await notification.DidNotReceive().PublishNackAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NACK PATH — Validation failure → compensate previous steps → Nack
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task RunAsync_ValidationFailure_CompensatesPriorSteps_PublishesNack()
    {
        if (SkipIfNoServer()) return;

        var validation = Substitute.For<IMessageValidationService>();
        validation.ValidateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(MessageValidationResult.Failure("Invalid JSON schema"));

        var logging = Substitute.For<IMessageLoggingService>();
        var persistence = Substitute.For<IPersistenceActivityService>();
        var notification = Substitute.For<INotificationActivityService>();
        var compensation = Substitute.For<ICompensationActivityService>();
        compensation.CompensateAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(true);

        var integrationActivities = new IntegrationActivities(validation, logging);
        var pipelineActivities = new PipelineActivities(persistence, notification, logging);
        var sagaActivities = new SagaCompensationActivities(compensation, logging);

        var input = BuildInput(payloadJson: "not-valid-json");

        using var worker = new TemporalWorker(
            _env!.Client,
            new TemporalWorkerOptions("test-atomic-queue")
                .AddWorkflow<AtomicPipelineWorkflow>()
                .AddAllActivities(integrationActivities)
                .AddAllActivities(pipelineActivities)
                .AddAllActivities(sagaActivities));

        var result = await worker.ExecuteAsync(() =>
            _env.Client.ExecuteWorkflowAsync(
                (AtomicPipelineWorkflow wf) => wf.RunAsync(input),
                new(id: $"test-atomic-nack-{Guid.NewGuid()}", taskQueue: "test-atomic-queue")));

        // ── Assert: Nack was published ──────────────────────────────────────
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Invalid JSON schema"));

        // ── Assert: Prior steps were compensated in reverse order ────────────
        // Steps completed before failure: PersistMessage, LogReceived
        // Compensation should be in reverse: LogReceived first, then PersistMessage
        Assert.That(result.CompensatedSteps, Is.Not.Null);
        Assert.That(result.CompensatedSteps, Has.Count.EqualTo(2));
        Assert.That(result.CompensatedSteps![0], Is.EqualTo("LogReceived"));
        Assert.That(result.CompensatedSteps[1], Is.EqualTo("PersistMessage"));

        // ── Assert: Compensation activities were called ─────────────────────
        await compensation.Received(1).CompensateAsync(input.CorrelationId, "LogReceived");
        await compensation.Received(1).CompensateAsync(input.CorrelationId, "PersistMessage");

        // ── Assert: Nack was published ──────────────────────────────────────
        await notification.Received(1).PublishNackAsync(
            input.MessageId, input.CorrelationId, "Invalid JSON schema", "integration.nack",
            Arg.Any<CancellationToken>());

        // ── Assert: Fault was saved ─────────────────────────────────────────
        await persistence.Received(1).SaveFaultAsync(
            input.MessageId, input.CorrelationId, "OrderCreated", "AtomicPipeline",
            "Invalid JSON schema", 0, Arg.Any<CancellationToken>());

        // ── Assert: No Ack was published ────────────────────────────────────
        await notification.DidNotReceive().PublishAckAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NACK PATH — Verify step execution order
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task RunAsync_ValidationFailure_ExecutesStepsInCorrectOrder()
    {
        if (SkipIfNoServer()) return;

        var executionOrder = new List<string>();

        var validation = Substitute.For<IMessageValidationService>();
        validation.ValidateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(MessageValidationResult.Failure("bad"));

        var logging = Substitute.For<IMessageLoggingService>();
        logging.LogAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Do<string>(s => executionOrder.Add($"Log:{s}")))
            .Returns(Task.CompletedTask);

        var persistence = Substitute.For<IPersistenceActivityService>();
        persistence.SaveMessageAsync(Arg.Any<IntegrationPipelineInput>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                executionOrder.Add("PersistMessage");
                return Task.CompletedTask;
            });

        var notification = Substitute.For<INotificationActivityService>();
        notification.PublishNackAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                executionOrder.Add("PublishNack");
                return Task.CompletedTask;
            });

        var compensation = Substitute.For<ICompensationActivityService>();
        compensation.CompensateAsync(Arg.Any<Guid>(), Arg.Do<string>(s => executionOrder.Add($"Compensate:{s}")))
            .Returns(true);

        var integrationActivities = new IntegrationActivities(validation, logging);
        var pipelineActivities = new PipelineActivities(persistence, notification, logging);
        var sagaActivities = new SagaCompensationActivities(compensation, logging);

        var input = BuildInput(payloadJson: "bad-json");

        using var worker = new TemporalWorker(
            _env!.Client,
            new TemporalWorkerOptions("test-atomic-queue")
                .AddWorkflow<AtomicPipelineWorkflow>()
                .AddAllActivities(integrationActivities)
                .AddAllActivities(pipelineActivities)
                .AddAllActivities(sagaActivities));

        await worker.ExecuteAsync(() =>
            _env.Client.ExecuteWorkflowAsync(
                (AtomicPipelineWorkflow wf) => wf.RunAsync(input),
                new(id: $"test-atomic-order-{Guid.NewGuid()}", taskQueue: "test-atomic-queue")));

        // Verify: Persist → Log:Received → [Validate fails] →
        //         Compensate:LogReceived → Compensate:PersistMessage → Nack
        Assert.That(executionOrder, Does.Contain("PersistMessage"));
        Assert.That(executionOrder, Does.Contain("Log:Received"));

        var compensateLogIdx = executionOrder.IndexOf("Compensate:LogReceived");
        var compensatePersistIdx = executionOrder.IndexOf("Compensate:PersistMessage");
        var nackIdx = executionOrder.IndexOf("PublishNack");

        Assert.That(compensateLogIdx, Is.GreaterThan(-1), "LogReceived was compensated");
        Assert.That(compensatePersistIdx, Is.GreaterThan(-1), "PersistMessage was compensated");
        Assert.That(compensateLogIdx, Is.LessThan(compensatePersistIdx),
            "LogReceived compensated before PersistMessage (reverse order)");
        Assert.That(nackIdx, Is.GreaterThan(compensatePersistIdx),
            "Nack published after all compensations");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NACK PATH — Partial compensation failure does not block Nack
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task RunAsync_CompensationPartiallyFails_StillPublishesNack()
    {
        if (SkipIfNoServer()) return;

        var validation = Substitute.For<IMessageValidationService>();
        validation.ValidateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(MessageValidationResult.Failure("bad"));

        var logging = Substitute.For<IMessageLoggingService>();
        var persistence = Substitute.For<IPersistenceActivityService>();
        var notification = Substitute.For<INotificationActivityService>();

        var compensation = Substitute.For<ICompensationActivityService>();
        // LogReceived compensation succeeds, PersistMessage compensation fails
        compensation.CompensateAsync(Arg.Any<Guid>(), "LogReceived").Returns(true);
        compensation.CompensateAsync(Arg.Any<Guid>(), "PersistMessage").Returns(false);

        var integrationActivities = new IntegrationActivities(validation, logging);
        var pipelineActivities = new PipelineActivities(persistence, notification, logging);
        var sagaActivities = new SagaCompensationActivities(compensation, logging);

        var input = BuildInput();

        using var worker = new TemporalWorker(
            _env!.Client,
            new TemporalWorkerOptions("test-atomic-queue")
                .AddWorkflow<AtomicPipelineWorkflow>()
                .AddAllActivities(integrationActivities)
                .AddAllActivities(pipelineActivities)
                .AddAllActivities(sagaActivities));

        var result = await worker.ExecuteAsync(() =>
            _env.Client.ExecuteWorkflowAsync(
                (AtomicPipelineWorkflow wf) => wf.RunAsync(input),
                new(id: $"test-atomic-partial-{Guid.NewGuid()}", taskQueue: "test-atomic-queue")));

        Assert.That(result.IsSuccess, Is.False);
        // Only LogReceived was successfully compensated (PersistMessage failed)
        Assert.That(result.CompensatedSteps, Has.Count.EqualTo(1));
        Assert.That(result.CompensatedSteps![0], Is.EqualTo("LogReceived"));

        // Nack still published even though PersistMessage compensation failed
        await notification.Received(1).PublishNackAsync(
            input.MessageId, input.CorrelationId, "bad", "integration.nack",
            Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SUCCESS PATH — Verify all activities called in correct order
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task RunAsync_Success_PersistLogValidateDeliverAck()
    {
        if (SkipIfNoServer()) return;

        var executionOrder = new List<string>();

        var validation = Substitute.For<IMessageValidationService>();
        validation.ValidateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(MessageValidationResult.Success);

        var logging = Substitute.For<IMessageLoggingService>();
        logging.LogAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Do<string>(s => executionOrder.Add($"Log:{s}")))
            .Returns(Task.CompletedTask);

        var persistence = Substitute.For<IPersistenceActivityService>();
        persistence.SaveMessageAsync(Arg.Any<IntegrationPipelineInput>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                executionOrder.Add("PersistMessage");
                return Task.CompletedTask;
            });
        persistence.UpdateDeliveryStatusAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Do<string>(s => executionOrder.Add($"UpdateStatus:{s}")),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var notification = Substitute.For<INotificationActivityService>();
        notification.PublishAckAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                executionOrder.Add("PublishAck");
                return Task.CompletedTask;
            });

        var compensation = Substitute.For<ICompensationActivityService>();

        var integrationActivities = new IntegrationActivities(validation, logging);
        var pipelineActivities = new PipelineActivities(persistence, notification, logging);
        var sagaActivities = new SagaCompensationActivities(compensation, logging);

        var input = BuildInput();

        using var worker = new TemporalWorker(
            _env!.Client,
            new TemporalWorkerOptions("test-atomic-queue")
                .AddWorkflow<AtomicPipelineWorkflow>()
                .AddAllActivities(integrationActivities)
                .AddAllActivities(pipelineActivities)
                .AddAllActivities(sagaActivities));

        await worker.ExecuteAsync(() =>
            _env.Client.ExecuteWorkflowAsync(
                (AtomicPipelineWorkflow wf) => wf.RunAsync(input),
                new(id: $"test-atomic-success-order-{Guid.NewGuid()}", taskQueue: "test-atomic-queue")));

        // Verify correct execution order
        Assert.That(executionOrder, Does.Contain("PersistMessage"));
        Assert.That(executionOrder, Does.Contain("Log:Received"));
        Assert.That(executionOrder, Does.Contain("UpdateStatus:Delivered"));
        Assert.That(executionOrder, Does.Contain("PublishAck"));

        var persistIdx = executionOrder.IndexOf("PersistMessage");
        var logReceivedIdx = executionOrder.IndexOf("Log:Received");
        var updateIdx = executionOrder.IndexOf("UpdateStatus:Delivered");
        var ackIdx = executionOrder.IndexOf("PublishAck");

        Assert.That(persistIdx, Is.LessThan(logReceivedIdx));
        Assert.That(logReceivedIdx, Is.LessThan(updateIdx));
        Assert.That(updateIdx, Is.LessThan(ackIdx));

        // No compensation occurred
        await compensation.DidNotReceiveWithAnyArgs().CompensateAsync(default, default!);
    }
}
