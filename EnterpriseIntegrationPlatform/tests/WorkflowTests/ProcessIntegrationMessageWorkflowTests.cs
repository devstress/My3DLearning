using NSubstitute;
using NUnit.Framework;

using Temporalio.Client;
using Temporalio.Testing;
using Temporalio.Worker;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Workflows;

namespace EnterpriseIntegrationPlatform.Tests.Workflow;

[TestFixture]
public class ProcessIntegrationMessageWorkflowTests 
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
            // Temporal dev server requires downloading a binary from the internet.
            // When the network is unavailable (CI sandbox, offline dev), skip tests.
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

    [Test]
    public async Task RunAsync_WithValidPayload_ReturnsSuccessResult()
    {
        if (SkipIfNoServer()) return;

        var validation = Substitute.For<IMessageValidationService>();
        validation.ValidateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(MessageValidationResult.Success);

        var logging = Substitute.For<IMessageLoggingService>();

        var activities = new IntegrationActivities(validation, logging);
        var input = new ProcessIntegrationMessageInput(
            Guid.NewGuid(), "OrderCreated", """{"orderId": 42}""");

        using var worker = new TemporalWorker(
            _env!.Client,
            new TemporalWorkerOptions("test-queue")
                .AddWorkflow<ProcessIntegrationMessageWorkflow>()
                .AddAllActivities(activities));

        var result = await worker.ExecuteAsync(() =>
            _env.Client.ExecuteWorkflowAsync(
                (ProcessIntegrationMessageWorkflow wf) => wf.RunAsync(input),
                new(id: $"test-valid-{Guid.NewGuid()}", taskQueue: "test-queue")));

        Assert.That(result.MessageId, Is.EqualTo(input.MessageId));
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FailureReason, Is.Null);
    }

    [Test]
    public async Task RunAsync_WithInvalidPayload_ReturnsFailureResult()
    {
        if (SkipIfNoServer()) return;

        var validation = Substitute.For<IMessageValidationService>();
        validation.ValidateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(MessageValidationResult.Failure("Payload is not valid JSON."));

        var logging = Substitute.For<IMessageLoggingService>();

        var activities = new IntegrationActivities(validation, logging);
        var input = new ProcessIntegrationMessageInput(
            Guid.NewGuid(), "OrderCreated", "not-json");

        using var worker = new TemporalWorker(
            _env!.Client,
            new TemporalWorkerOptions("test-queue")
                .AddWorkflow<ProcessIntegrationMessageWorkflow>()
                .AddAllActivities(activities));

        var result = await worker.ExecuteAsync(() =>
            _env.Client.ExecuteWorkflowAsync(
                (ProcessIntegrationMessageWorkflow wf) => wf.RunAsync(input),
                new(id: $"test-invalid-{Guid.NewGuid()}", taskQueue: "test-queue")));

        Assert.That(result.MessageId, Is.EqualTo(input.MessageId));
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Payload is not valid JSON."));
    }

    [Test]
    public async Task RunAsync_LogsReceivedStage_BeforeValidation()
    {
        if (SkipIfNoServer()) return;

        var validation = Substitute.For<IMessageValidationService>();
        validation.ValidateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(MessageValidationResult.Success);

        var logging = Substitute.For<IMessageLoggingService>();
        var stages = new List<string>();
        logging.LogAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Do<string>(s => stages.Add(s)))
            .Returns(Task.CompletedTask);

        var activities = new IntegrationActivities(validation, logging);
        var messageId = Guid.NewGuid();
        var input = new ProcessIntegrationMessageInput(
            messageId, "ShipmentCreated", """{"shipmentId": 7}""");

        using var worker = new TemporalWorker(
            _env!.Client,
            new TemporalWorkerOptions("test-queue")
                .AddWorkflow<ProcessIntegrationMessageWorkflow>()
                .AddAllActivities(activities));

        await worker.ExecuteAsync(() =>
            _env.Client.ExecuteWorkflowAsync(
                (ProcessIntegrationMessageWorkflow wf) => wf.RunAsync(input),
                new(id: $"test-stages-{Guid.NewGuid()}", taskQueue: "test-queue")));

        Assert.That(stages, Is.EqualTo(new[] { "Received", "Validated" }));
    }

    [Test]
    public async Task RunAsync_OnValidationFailure_LogsValidationFailedStage()
    {
        if (SkipIfNoServer()) return;

        var validation = Substitute.For<IMessageValidationService>();
        validation.ValidateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(MessageValidationResult.Failure("bad"));

        var logging = Substitute.For<IMessageLoggingService>();
        var stages = new List<string>();
        logging.LogAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Do<string>(s => stages.Add(s)))
            .Returns(Task.CompletedTask);

        var activities = new IntegrationActivities(validation, logging);
        var input = new ProcessIntegrationMessageInput(
            Guid.NewGuid(), "OrderCreated", "bad");

        using var worker = new TemporalWorker(
            _env!.Client,
            new TemporalWorkerOptions("test-queue")
                .AddWorkflow<ProcessIntegrationMessageWorkflow>()
                .AddAllActivities(activities));

        await worker.ExecuteAsync(() =>
            _env.Client.ExecuteWorkflowAsync(
                (ProcessIntegrationMessageWorkflow wf) => wf.RunAsync(input),
                new(id: $"test-fail-stages-{Guid.NewGuid()}", taskQueue: "test-queue")));

        Assert.That(stages, Is.EqualTo(new[] { "Received", "ValidationFailed" }));
        Assert.That(stages, Does.Not.Contain("Validated"));
    }
}
