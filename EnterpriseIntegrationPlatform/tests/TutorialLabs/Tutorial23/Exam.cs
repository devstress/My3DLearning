// ============================================================================
// Tutorial 23 – Request-Reply (Exam)
// ============================================================================
// Coding challenges: validate that empty ReplyTopic throws, verify the
// correlator subscribes on the reply topic before publishing, and test
// the generated correlationId flow when none is provided.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.RequestReply;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial23;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Empty ReplyTopic Throws ─────────────────────────────────

    [Test]
    public void Challenge1_EmptyReplyTopic_ThrowsArgumentException()
    {
        // When ReplyTopic is empty or whitespace, the correlator should throw
        // before publishing anything.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var consumer = Substitute.For<IMessageBrokerConsumer>();
        var options = Options.Create(new RequestReplyOptions { TimeoutMs = 500 });

        var correlator = new RequestReplyCorrelator<string, string>(
            producer, consumer, options,
            NullLogger<RequestReplyCorrelator<string, string>>.Instance);

        var msg = new RequestReplyMessage<string>(
            "data", "cmd-topic", "  ", "Svc", "type");

        Assert.ThrowsAsync<ArgumentException>(
            () => correlator.SendAndReceiveAsync(msg));
    }

    // ── Challenge 2: Consumer Subscribes On Reply Topic ─────────────────────

    [Test]
    public async Task Challenge2_Correlator_SubscribesOnReplyTopic()
    {
        // Verify the correlator subscribes to the correct reply topic with
        // the consumer group from options.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var consumer = Substitute.For<IMessageBrokerConsumer>();
        var options = Options.Create(new RequestReplyOptions
        {
            TimeoutMs = 300,
            ConsumerGroup = "my-group",
        });

        var correlator = new RequestReplyCorrelator<string, string>(
            producer, consumer, options,
            NullLogger<RequestReplyCorrelator<string, string>>.Instance);

        var msg = new RequestReplyMessage<string>(
            "payload", "commands", "my-replies", "Svc", "cmd.ping");

        await correlator.SendAndReceiveAsync(msg);

        await consumer.Received(1).SubscribeAsync<string>(
            "my-replies",
            "my-group",
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());
    }

    // ── Challenge 3: Auto-Generated CorrelationId On Result ─────────────────

    [Test]
    public async Task Challenge3_NullCorrelationId_GeneratesNewOne()
    {
        // When no CorrelationId is provided in the message, the correlator
        // generates a new one. The result should carry a non-empty CorrelationId.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var consumer = Substitute.For<IMessageBrokerConsumer>();
        var options = Options.Create(new RequestReplyOptions { TimeoutMs = 300 });

        var correlator = new RequestReplyCorrelator<string, string>(
            producer, consumer, options,
            NullLogger<RequestReplyCorrelator<string, string>>.Instance);

        var msg = new RequestReplyMessage<string>(
            "data", "topic-a", "reply-a", "Svc", "type", CorrelationId: null);

        var result = await correlator.SendAndReceiveAsync(msg);

        Assert.That(result.CorrelationId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.TimedOut, Is.True);
    }
}
