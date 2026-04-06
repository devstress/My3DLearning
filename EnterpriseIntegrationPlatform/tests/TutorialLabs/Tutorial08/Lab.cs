// ============================================================================
// Tutorial 08 – Activities and Pipeline (Lab)
// ============================================================================
// This lab verifies the platform's Activity classes, exercises the pipeline
// concept (create → validate → transform → route) using mocked services,
// and chains multiple activity calls in sequence.
// ============================================================================

using System.Reflection;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Workflow.Temporal;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial08;

[TestFixture]
public sealed class Lab
{
    // ── Verifying Activity Types Exist ───────────────────────────────────────

    [Test]
    public void IntegrationActivities_ClassExists_WithExpectedMethods()
    {
        // IntegrationActivities is the Temporal activity class that wraps
        // validation and logging services.
        var assembly = typeof(TemporalOptions).Assembly;
        var activityType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "IntegrationActivities");

        Assert.That(activityType, Is.Not.Null,
            "IntegrationActivities should exist in Workflow.Temporal");

        Assert.That(activityType!.GetMethod("ValidateMessageAsync"), Is.Not.Null);
        Assert.That(activityType.GetMethod("LogProcessingStageAsync"), Is.Not.Null);
    }

    [Test]
    public void PipelineActivities_ClassExists_WithExpectedMethods()
    {
        // PipelineActivities wraps persistence and notification as activities.
        var assembly = typeof(TemporalOptions).Assembly;
        var activityType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "PipelineActivities");

        Assert.That(activityType, Is.Not.Null,
            "PipelineActivities should exist in Workflow.Temporal");

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

    [Test]
    public void SagaCompensationActivities_ClassExists()
    {
        var assembly = typeof(TemporalOptions).Assembly;
        var activityType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "SagaCompensationActivities");

        Assert.That(activityType, Is.Not.Null,
            "SagaCompensationActivities should exist in Workflow.Temporal");

        Assert.That(activityType!.GetMethod("CompensateStepAsync"), Is.Not.Null);
    }

    // ── Pipeline Concept: Create → Validate → Transform → Route ─────────────

    [Test]
    public async Task Pipeline_CreateValidateTransformRoute_AllStepsExecute()
    {
        // Simulate the full pipeline pattern using mocked services:
        //   1. Create an envelope (the message entering the pipeline)
        //   2. Validate the message payload
        //   3. Transform: add routing metadata
        //   4. Route: publish to a destination topic
        var validationService = Substitute.For<IMessageValidationService>();
        var loggingService = Substitute.For<IMessageLoggingService>();
        var producer = Substitute.For<IMessageBrokerProducer>();

        var messageId = Guid.NewGuid();
        const string messageType = "order.created";
        const string payloadJson = "{\"orderId\": \"ORD-500\"}";

        // Step 1: Create envelope.
        var envelope = IntegrationEnvelope<string>.Create(
            payloadJson, "OrderService", messageType) with
        {
            Intent = MessageIntent.Command,
        };

        Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));

        // Step 2: Validate.
        validationService.ValidateAsync(messageType, payloadJson)
            .Returns(MessageValidationResult.Success);

        var validationResult = await validationService.ValidateAsync(messageType, payloadJson);
        Assert.That(validationResult.IsValid, Is.True);

        // Step 3: Transform — enrich metadata with a routing hint.
        envelope = envelope with
        {
            Metadata = new Dictionary<string, string>(envelope.Metadata)
            {
                ["region"] = "us-east",
                ["validated"] = "true",
            },
        };

        Assert.That(envelope.Metadata["region"], Is.EqualTo("us-east"));

        // Step 4: Route — publish to destination topic.
        await producer.PublishAsync(envelope, "orders.us-east");

        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(
                e => e.Metadata.ContainsKey("region") && e.Metadata["region"] == "us-east"),
            Arg.Is("orders.us-east"),
            Arg.Any<CancellationToken>());
    }

    // ── Chaining Multiple Activity Calls ────────────────────────────────────

    [Test]
    public async Task ChainedActivities_PersistLogValidateLog_InSequence()
    {
        // Simulate the IntegrationPipelineWorkflow's activity chain:
        //   Persist → Log(Received) → Validate → Log(Validated or Failed)
        var persistenceService = Substitute.For<IPersistenceActivityService>();
        var loggingService = Substitute.For<IMessageLoggingService>();
        var validationService = Substitute.For<IMessageValidationService>();

        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            Timestamp: DateTimeOffset.UtcNow,
            Source: "Lab08",
            MessageType: "lab.pipeline",
            SchemaVersion: "1.0",
            Priority: 1,
            PayloadJson: "{\"item\": \"widget\"}",
            MetadataJson: null,
            AckSubject: "ack.lab08",
            NackSubject: "nack.lab08");

        // Configure mocks.
        persistenceService.SaveMessageAsync(input, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        loggingService.LogAsync(input.MessageId, input.MessageType, Arg.Any<string>())
            .Returns(Task.CompletedTask);
        validationService.ValidateAsync(input.MessageType, input.PayloadJson)
            .Returns(MessageValidationResult.Success);

        // Execute chain.
        await persistenceService.SaveMessageAsync(input);
        await loggingService.LogAsync(input.MessageId, input.MessageType, "Received");
        var result = await validationService.ValidateAsync(input.MessageType, input.PayloadJson);
        await loggingService.LogAsync(input.MessageId, input.MessageType,
            result.IsValid ? "Validated" : "ValidationFailed");

        // Verify execution order.
        Received.InOrder(() =>
        {
            persistenceService.SaveMessageAsync(input, Arg.Any<CancellationToken>());
            loggingService.LogAsync(input.MessageId, input.MessageType, "Received");
            validationService.ValidateAsync(input.MessageType, input.PayloadJson);
            loggingService.LogAsync(input.MessageId, input.MessageType, "Validated");
        });
    }
}
