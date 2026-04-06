// ============================================================================
// Tutorial 07 – Temporal Workflows (Lab)
// ============================================================================
// This lab uses reflection to verify that the Temporal workflow infrastructure
// exists in the platform, inspects configuration types, and demonstrates a
// mocked workflow activity chain concept.
// ============================================================================

using System.Reflection;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial07;

[TestFixture]
public sealed class Lab
{
    // ── Verifying Temporal Workflow Types via Reflection ─────────────────────

    [Test]
    public void TemporalWorkflows_ProcessIntegrationMessage_Exists()
    {
        // The platform defines a ProcessIntegrationMessageWorkflow in the
        // Workflow.Temporal assembly.  Verify it exists via reflection.
        var assembly = typeof(TemporalOptions).Assembly;
        var workflowType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ProcessIntegrationMessageWorkflow");

        Assert.That(workflowType, Is.Not.Null,
            "ProcessIntegrationMessageWorkflow should exist in the Workflow.Temporal assembly");
        Assert.That(workflowType!.IsClass, Is.True);
    }

    [Test]
    public void TemporalWorkflows_IntegrationPipelineWorkflow_Exists()
    {
        // The full pipeline workflow: persist → validate → ack/nack.
        var assembly = typeof(TemporalOptions).Assembly;
        var workflowType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "IntegrationPipelineWorkflow");

        Assert.That(workflowType, Is.Not.Null,
            "IntegrationPipelineWorkflow should exist");
    }

    [Test]
    public void TemporalWorkflows_SagaCompensationWorkflow_Exists()
    {
        // The saga compensation workflow for rollback scenarios.
        var assembly = typeof(TemporalOptions).Assembly;
        var workflowType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "SagaCompensationWorkflow");

        Assert.That(workflowType, Is.Not.Null,
            "SagaCompensationWorkflow should exist");
    }

    [Test]
    public void TemporalWorkflows_AtomicPipelineWorkflow_Exists()
    {
        // The atomic variant adds saga compensation on top of the pipeline.
        var assembly = typeof(TemporalOptions).Assembly;
        var workflowType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AtomicPipelineWorkflow");

        Assert.That(workflowType, Is.Not.Null,
            "AtomicPipelineWorkflow should exist");
    }

    // ── Verifying Workflow Configuration Types ──────────────────────────────

    [Test]
    public void TemporalOptions_HasExpectedDefaults()
    {
        // TemporalOptions configures the Temporal worker host.
        var options = new TemporalOptions();

        Assert.That(options.ServerAddress, Is.EqualTo("localhost:15233"));
        Assert.That(options.Namespace, Is.EqualTo("default"));
        Assert.That(options.TaskQueue, Is.EqualTo("integration-workflows"));
        Assert.That(TemporalOptions.SectionName, Is.EqualTo("Temporal"));
    }

    [Test]
    public void TemporalOptions_CanOverrideSettings()
    {
        var options = new TemporalOptions
        {
            ServerAddress = "temporal.prod.internal:7233",
            Namespace = "production",
            TaskQueue = "prod-integration",
        };

        Assert.That(options.ServerAddress, Is.EqualTo("temporal.prod.internal:7233"));
        Assert.That(options.Namespace, Is.EqualTo("production"));
        Assert.That(options.TaskQueue, Is.EqualTo("prod-integration"));
    }

    // ── Verifying Temporal Activity Classes via Reflection ───────────────────

    [Test]
    public void TemporalActivities_IntegrationActivities_HasExpectedMethods()
    {
        // IntegrationActivities wraps validation and logging as Temporal activities.
        var assembly = typeof(TemporalOptions).Assembly;
        var activityType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "IntegrationActivities");

        Assert.That(activityType, Is.Not.Null);

        var validateMethod = activityType!.GetMethod("ValidateMessageAsync");
        Assert.That(validateMethod, Is.Not.Null,
            "ValidateMessageAsync activity should exist");

        var logMethod = activityType.GetMethod("LogProcessingStageAsync");
        Assert.That(logMethod, Is.Not.Null,
            "LogProcessingStageAsync activity should exist");
    }

    [Test]
    public void TemporalActivities_PipelineActivities_HasExpectedMethods()
    {
        // PipelineActivities wraps persistence and notification as Temporal activities.
        var assembly = typeof(TemporalOptions).Assembly;
        var activityType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "PipelineActivities");

        Assert.That(activityType, Is.Not.Null);

        var methodNames = activityType!.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToList();

        Assert.That(methodNames, Does.Contain("PersistMessageAsync"));
        Assert.That(methodNames, Does.Contain("UpdateDeliveryStatusAsync"));
        Assert.That(methodNames, Does.Contain("SaveFaultAsync"));
        Assert.That(methodNames, Does.Contain("PublishAckAsync"));
        Assert.That(methodNames, Does.Contain("PublishNackAsync"));
        Assert.That(methodNames, Does.Contain("LogStageAsync"));
    }

    // ── Mock Workflow Scenario: Activity Chain Concept ───────────────────────

    [Test]
    public async Task MockWorkflowScenario_ValidateTransformRoute_ChainSucceeds()
    {
        // Demonstrate the activity chain concept that Temporal orchestrates:
        // Step 1: Validate → Step 2: Log stage → Step 3: Route decision.
        // We mock the services that back the activities.
        var validationService = Substitute.For<IMessageValidationService>();
        var loggingService = Substitute.For<IMessageLoggingService>();

        var messageId = Guid.NewGuid();
        const string messageType = "order.created";
        const string payloadJson = "{\"orderId\": \"ORD-001\"}";

        // Step 1: Validation succeeds.
        validationService.ValidateAsync(messageType, payloadJson)
            .Returns(MessageValidationResult.Success);

        // Step 2: Logging completes.
        loggingService.LogAsync(messageId, messageType, Arg.Any<string>())
            .Returns(Task.CompletedTask);

        // Execute the chain.
        var validationResult = await validationService.ValidateAsync(messageType, payloadJson);
        Assert.That(validationResult.IsValid, Is.True);

        await loggingService.LogAsync(messageId, messageType, "Validated");

        // Verify the chain executed in order.
        await validationService.Received(1).ValidateAsync(messageType, payloadJson);
        await loggingService.Received(1).LogAsync(messageId, messageType, "Validated");
    }
}
