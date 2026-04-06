// ============================================================================
// Tutorial 08 – Activities Pipeline (Lab)
// ============================================================================
// EIP Pattern: Pipes and Filters.
// E2E: Build pipeline with real DefaultMessageValidationService + mocked
// services, execute pipeline stages, verify each stage processes correctly.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial08;

[TestFixture]
public sealed class Lab
{
    [Test]
    public async Task ValidationStage_ValidPayload_Succeeds()
    {
        var validator = new DefaultMessageValidationService();

        var result = await validator.ValidateAsync("order.created", "{\"orderId\":\"ORD-1\"}");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reason, Is.Null);
    }

    [Test]
    public async Task ValidationStage_EmptyPayload_Fails()
    {
        var validator = new DefaultMessageValidationService();

        var result = await validator.ValidateAsync("order.created", "");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Does.Contain("empty"));
    }

    [Test]
    public async Task ValidationStage_NonJsonPayload_Fails()
    {
        var validator = new DefaultMessageValidationService();

        var result = await validator.ValidateAsync("order.created", "not-json");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Does.Contain("JSON"));
    }

    [Test]
    public async Task PipelineChain_ValidateAndPublish_EndToEnd()
    {
        var validator = new DefaultMessageValidationService();
        await using var output = new MockEndpoint("output");

        var envelope = IntegrationEnvelope<string>.Create(
            "{\"item\":\"widget\"}", "FactoryService", "factory.produced");

        var result = await validator.ValidateAsync(
            envelope.MessageType, envelope.Payload);
        Assert.That(result.IsValid, Is.True);

        var channel = new PointToPointChannel(
            output, output, NullLogger<PointToPointChannel>.Instance);
        await channel.SendAsync(envelope, "validated-queue", CancellationToken.None);

        output.AssertReceivedCount(1);
        Assert.That(output.GetReceived<string>().Payload, Does.Contain("widget"));
    }

    [Test]
    public async Task PipelineChain_ValidationFails_RoutesToInvalidChannel()
    {
        var validator = new DefaultMessageValidationService();
        await using var output = new MockEndpoint("invalid-output");

        var envelope = IntegrationEnvelope<string>.Create(
            "bad-data", "LegacySystem", "legacy.event");

        var result = await validator.ValidateAsync(
            envelope.MessageType, envelope.Payload);
        Assert.That(result.IsValid, Is.False);

        var invalidOptions = Options.Create(new InvalidMessageChannelOptions
            { InvalidMessageTopic = "invalid-msgs", Source = "Pipeline" });
        var invalidChannel = new InvalidMessageChannel(
            output, invalidOptions, NullLogger<InvalidMessageChannel>.Instance);

        await invalidChannel.RouteInvalidAsync(
            envelope, result.Reason!, CancellationToken.None);

        output.AssertReceivedCount(1);
        output.AssertReceivedOnTopic("invalid-msgs", 1);
    }

    [Test]
    public async Task PipelineChain_PersistThenValidateThenPublish()
    {
        var persistence = new MockPersistenceActivityService();
        var validator = new DefaultMessageValidationService();
        await using var output = new MockEndpoint("pipeline-out");

        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(), CorrelationId: Guid.NewGuid(),
            CausationId: null, Timestamp: DateTimeOffset.UtcNow,
            Source: "Lab08", MessageType: "lab.event", SchemaVersion: "1.0",
            Priority: 1, PayloadJson: "{\"data\":true}", MetadataJson: null,
            AckSubject: "ack", NackSubject: "nack");

        await persistence.SaveMessageAsync(input);
        persistence.AssertSaveCount(1);

        var validation = await validator.ValidateAsync(input.MessageType, input.PayloadJson);
        Assert.That(validation.IsValid, Is.True);

        var envelope = IntegrationEnvelope<string>.Create(
            input.PayloadJson, input.Source, input.MessageType);
        await output.PublishAsync(envelope, "processed-topic");

        output.AssertReceivedCount(1);
        output.AssertReceivedOnTopic("processed-topic", 1);
    }
}
