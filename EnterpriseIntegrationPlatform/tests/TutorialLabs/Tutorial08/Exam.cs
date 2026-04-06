// ============================================================================
// Tutorial 08 – Activities Pipeline (Exam)
// ============================================================================
// E2E challenges: full pipeline with enrichment, DLQ routing on failure,
// and multi-stage pipeline with MockEndpoint verification.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial08;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_EnrichAndPublish_MetadataPreserved()
    {
        var validator = new DefaultMessageValidationService();
        await using var output = new MockEndpoint("enriched");

        var envelope = IntegrationEnvelope<string>.Create(
            "{\"orderId\":\"ORD-42\"}", "OrderService", "order.created");

        var result = await validator.ValidateAsync(
            envelope.MessageType, envelope.Payload);
        Assert.That(result.IsValid, Is.True);

        var enriched = envelope with
        {
            Metadata = new Dictionary<string, string>(envelope.Metadata)
            {
                ["processed-by"] = "Pipeline",
                ["region"] = "us-east",
            },
        };

        var channel = new PointToPointChannel(
            output, output, NullLogger<PointToPointChannel>.Instance);
        await channel.SendAsync(enriched, "enriched-queue", CancellationToken.None);

        output.AssertReceivedCount(1);
        var received = output.GetReceived<string>();
        Assert.That(received.Metadata["processed-by"], Is.EqualTo("Pipeline"));
        Assert.That(received.Metadata["region"], Is.EqualTo("us-east"));
    }

    [Test]
    public async Task Challenge2_ValidationFailure_RoutesDlqAndSkipsOutput()
    {
        var validator = new DefaultMessageValidationService();
        await using var goodOutput = new MockEndpoint("good");
        await using var dlqOutput = new MockEndpoint("dlq");

        var envelope = IntegrationEnvelope<string>.Create(
            "not-json", "LegacySystem", "legacy.event");

        var result = await validator.ValidateAsync(
            envelope.MessageType, envelope.Payload);

        if (result.IsValid)
        {
            await goodOutput.PublishAsync(envelope, "good-topic");
        }
        else
        {
            var invalidOpts = Options.Create(new InvalidMessageChannelOptions
                { InvalidMessageTopic = "dlq-topic", Source = "Pipeline" });
            var invalidChannel = new InvalidMessageChannel(
                dlqOutput, invalidOpts, NullLogger<InvalidMessageChannel>.Instance);
            await invalidChannel.RouteInvalidAsync(
                envelope, result.Reason!, CancellationToken.None);
        }

        goodOutput.AssertNoneReceived();
        dlqOutput.AssertReceivedCount(1);
        dlqOutput.AssertReceivedOnTopic("dlq-topic", 1);
    }

    [Test]
    public async Task Challenge3_MultiStage_PersistValidatePublishVerify()
    {
        var persistence = Substitute.For<IPersistenceActivityService>();
        var logging = Substitute.For<IMessageLoggingService>();
        var validator = new DefaultMessageValidationService();
        await using var output = new MockEndpoint("final");

        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(), CorrelationId: Guid.NewGuid(),
            CausationId: null, Timestamp: DateTimeOffset.UtcNow,
            Source: "ExamService", MessageType: "exam.event", SchemaVersion: "1.0",
            Priority: 2, PayloadJson: "{\"exam\":true}", MetadataJson: null,
            AckSubject: "ack.exam", NackSubject: "nack.exam");

        await persistence.SaveMessageAsync(input);
        await logging.LogAsync(input.MessageId, input.MessageType, "Persisted");

        var validation = await validator.ValidateAsync(
            input.MessageType, input.PayloadJson);
        Assert.That(validation.IsValid, Is.True);
        await logging.LogAsync(input.MessageId, input.MessageType, "Validated");

        var envelope = IntegrationEnvelope<string>.Create(
            input.PayloadJson, input.Source, input.MessageType);
        await output.PublishAsync(envelope, "final-topic");
        await logging.LogAsync(input.MessageId, input.MessageType, "Published");

        output.AssertReceivedCount(1);
        await persistence.Received(1).SaveMessageAsync(input, Arg.Any<CancellationToken>());
        await logging.Received(1).LogAsync(input.MessageId, input.MessageType, "Persisted");
        await logging.Received(1).LogAsync(input.MessageId, input.MessageType, "Validated");
        await logging.Received(1).LogAsync(input.MessageId, input.MessageType, "Published");
    }
}
