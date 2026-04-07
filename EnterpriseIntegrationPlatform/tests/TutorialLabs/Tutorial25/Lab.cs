// ============================================================================
// Tutorial 25 – Dead Letter Queue (Lab)
// ============================================================================
// EIP Pattern: Dead Letter Channel.
// E2E: Wire real DeadLetterPublisher with MockEndpoint as producer.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial25;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("dlq-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();


    // ── 1. Core DLQ Publishing ───────────────────────────────────────

    [Test]
    public async Task Publish_MaxRetriesExceeded_SendsToDeadLetterTopic()
    {
        var publisher = CreatePublisher();
        var envelope = IntegrationEnvelope<string>.Create("order-data", "OrderSvc", "order.created");

        await publisher.PublishAsync(envelope, DeadLetterReason.MaxRetriesExceeded,
            "Max retries exceeded", 3, CancellationToken.None);

        _output.AssertReceivedOnTopic("dlq-topic", 1);
    }

    [Test]
    public async Task Publish_PreservesOriginalEnvelope()
    {
        var publisher = CreatePublisher();
        var envelope = IntegrationEnvelope<string>.Create("payload", "Svc", "type");

        await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage,
            "Unprocessable", 1, CancellationToken.None);

        var received = _output.GetReceived<DeadLetterEnvelope<string>>(0);
        Assert.That(received.Payload.OriginalEnvelope.Payload, Is.EqualTo("payload"));
        Assert.That(received.Payload.OriginalEnvelope.MessageId, Is.EqualTo(envelope.MessageId));
    }


    // ── 2. Dead-Letter Metadata ──────────────────────────────────────

    [Test]
    public async Task Publish_SetsCorrectReason()
    {
        var publisher = CreatePublisher();
        var envelope = IntegrationEnvelope<string>.Create("data", "svc", "type");

        await publisher.PublishAsync(envelope, DeadLetterReason.ValidationFailed,
            "Schema invalid", 1, CancellationToken.None);

        var received = _output.GetReceived<DeadLetterEnvelope<string>>(0);
        Assert.That(received.Payload.Reason, Is.EqualTo(DeadLetterReason.ValidationFailed));
        Assert.That(received.Payload.ErrorMessage, Is.EqualTo("Schema invalid"));
    }

    [Test]
    public async Task Publish_TracksAttemptCount()
    {
        var publisher = CreatePublisher();
        var envelope = IntegrationEnvelope<string>.Create("data", "svc", "type");

        await publisher.PublishAsync(envelope, DeadLetterReason.ProcessingTimeout,
            "Timed out", 5, CancellationToken.None);

        var received = _output.GetReceived<DeadLetterEnvelope<string>>(0);
        Assert.That(received.Payload.AttemptCount, Is.EqualTo(5));
    }

    [Test]
    public async Task Publish_SetsFailedAtTimestamp()
    {
        var publisher = CreatePublisher();
        var envelope = IntegrationEnvelope<string>.Create("data", "svc", "type");
        var before = DateTimeOffset.UtcNow;

        await publisher.PublishAsync(envelope, DeadLetterReason.UnroutableMessage,
            "No route", 1, CancellationToken.None);

        var received = _output.GetReceived<DeadLetterEnvelope<string>>(0);
        Assert.That(received.Payload.FailedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(received.Payload.FailedAt, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }


    // ── 3. Reason Coverage ───────────────────────────────────────────

    [Test]
    public async Task Publish_PreservesCorrelationId()
    {
        var publisher = CreatePublisher();
        var correlationId = Guid.NewGuid();
        var envelope = IntegrationEnvelope<string>.Create("data", "svc", "type", correlationId);

        await publisher.PublishAsync(envelope, DeadLetterReason.MaxRetriesExceeded,
            "Exhausted", 3, CancellationToken.None);

        var received = _output.GetReceived<DeadLetterEnvelope<string>>(0);
        Assert.That(received.CorrelationId, Is.EqualTo(correlationId));
    }

    [Test]
    public async Task Publish_AllReasonValues_AreSupported()
    {
        var publisher = CreatePublisher();

        var reasons = new[]
        {
            DeadLetterReason.MaxRetriesExceeded,
            DeadLetterReason.PoisonMessage,
            DeadLetterReason.ProcessingTimeout,
            DeadLetterReason.ValidationFailed,
            DeadLetterReason.UnroutableMessage,
            DeadLetterReason.MessageExpired,
        };

        foreach (var reason in reasons)
        {
            var envelope = IntegrationEnvelope<string>.Create("data", "svc", "type");
            await publisher.PublishAsync(envelope, reason, $"Error: {reason}", 1, CancellationToken.None);
        }

        _output.AssertReceivedOnTopic("dlq-topic", reasons.Length);
    }

    private DeadLetterPublisher<string> CreatePublisher()
    {
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "dlq-topic",
        });

        return new DeadLetterPublisher<string>(_output, options);
    }
}
