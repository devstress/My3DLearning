using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class DeadLetterPublisherTests
{
    private readonly IMessageBrokerProducer _producer;

    public DeadLetterPublisherTests()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private DeadLetterPublisher<string> BuildPublisher(DeadLetterOptions? options = null)
    {
        options ??= new DeadLetterOptions { DeadLetterTopic = "dlq.test" };
        return new DeadLetterPublisher<string>(_producer, Options.Create(options));
    }

    private static IntegrationEnvelope<string> BuildEnvelope(string payload = "test")
        => IntegrationEnvelope<string>.Create(payload, "TestService", "TestEvent");

    [Test]
    public async Task PublishAsync_ValidEnvelope_PublishesToCorrectTopic()
    {
        var publisher = BuildPublisher(new DeadLetterOptions { DeadLetterTopic = "dlq.orders" });
        var envelope = BuildEnvelope();
        string? capturedTopic = null;
        await _producer.PublishAsync(
            Arg.Any<IntegrationEnvelope<DeadLetterEnvelope<string>>>(),
            Arg.Do<string>(t => capturedTopic = t),
            Arg.Any<CancellationToken>());

        await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage, "error", 1, CancellationToken.None);

        Assert.That(capturedTopic, Is.EqualTo("dlq.orders"));
    }

    [Test]
    public async Task PublishAsync_ValidEnvelope_WrapsOriginalEnvelope()
    {
        var publisher = BuildPublisher();
        var envelope = BuildEnvelope("original-payload");
        IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await publisher.PublishAsync(envelope, DeadLetterReason.MaxRetriesExceeded, "error", 1, CancellationToken.None);

        Assert.That(captured!.Payload.OriginalEnvelope.Payload, Is.EqualTo("original-payload"));
    }

    [Test]
    public async Task PublishAsync_ValidEnvelope_PreservesCorrelationId()
    {
        var publisher = BuildPublisher();
        var envelope = BuildEnvelope();
        IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage, "error", 1, CancellationToken.None);

        Assert.That(captured!.CorrelationId, Is.EqualTo(envelope.CorrelationId));
    }

    [Test]
    public async Task PublishAsync_ValidEnvelope_SetsCausationIdToOriginalMessageId()
    {
        var publisher = BuildPublisher();
        var envelope = BuildEnvelope();
        IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage, "error", 1, CancellationToken.None);

        Assert.That(captured!.CausationId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task PublishAsync_ValidEnvelope_GeneratesNewMessageId()
    {
        var publisher = BuildPublisher();
        var envelope = BuildEnvelope();
        IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage, "error", 1, CancellationToken.None);

        Assert.That(captured!.MessageId, Is.Not.EqualTo(envelope.MessageId));
        Assert.That(captured!.MessageId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task PublishAsync_OptionsHasSource_UsesOptionsSource()
    {
        var publisher = BuildPublisher(new DeadLetterOptions { DeadLetterTopic = "dlq", Source = "OverrideSource" });
        var envelope = BuildEnvelope();
        IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage, "error", 1, CancellationToken.None);

        Assert.That(captured!.Source, Is.EqualTo("OverrideSource"));
    }

    [Test]
    public async Task PublishAsync_OptionsHasMessageType_UsesOptionsMessageType()
    {
        var publisher = BuildPublisher(new DeadLetterOptions { DeadLetterTopic = "dlq", MessageType = "CustomDLQ" });
        var envelope = BuildEnvelope();
        IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage, "error", 1, CancellationToken.None);

        Assert.That(captured!.MessageType, Is.EqualTo("CustomDLQ"));
    }

    [Test]
    public async Task PublishAsync_ValidEnvelope_SetsReasonAndErrorMessage()
    {
        var publisher = BuildPublisher();
        var envelope = BuildEnvelope();
        IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await publisher.PublishAsync(envelope, DeadLetterReason.ValidationFailed, "validation error", 2, CancellationToken.None);

        Assert.That(captured!.Payload.Reason, Is.EqualTo(DeadLetterReason.ValidationFailed));
        Assert.That(captured!.Payload.ErrorMessage, Is.EqualTo("validation error"));
    }

    [Test]
    public async Task PublishAsync_ValidEnvelope_FailedAtIsRecentUtc()
    {
        var publisher = BuildPublisher();
        var envelope = BuildEnvelope();
        var before = DateTimeOffset.UtcNow;
        IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage, "error", 1, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.That(captured!.Payload.FailedAt, Is.GreaterThanOrEqualTo(before).And.LessThanOrEqualTo(after));
    }

    [Test]
    public async Task PublishAsync_ValidEnvelope_SetsAttemptCount()
    {
        var publisher = BuildPublisher();
        var envelope = BuildEnvelope();
        IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await publisher.PublishAsync(envelope, DeadLetterReason.MaxRetriesExceeded, "error", 5, CancellationToken.None);

        Assert.That(captured!.Payload.AttemptCount, Is.EqualTo(5));
    }

    [Test]
    public async Task PublishAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var publisher = BuildPublisher();

        var act = async () => await publisher.PublishAsync(null!, DeadLetterReason.PoisonMessage, "error", 1, CancellationToken.None);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }

    [Test]
    public async Task PublishAsync_EmptyDeadLetterTopic_ThrowsInvalidOperationException()
    {
        var publisher = BuildPublisher(new DeadLetterOptions { DeadLetterTopic = "" });
        var envelope = BuildEnvelope();

        var act = async () => await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage, "error", 1, CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await act());
    }

    [Test]
    [TestCase(DeadLetterReason.MaxRetriesExceeded)]
    [TestCase(DeadLetterReason.PoisonMessage)]
    [TestCase(DeadLetterReason.ProcessingTimeout)]
    [TestCase(DeadLetterReason.ValidationFailed)]
    [TestCase(DeadLetterReason.UnroutableMessage)]
    public async Task PublishAsync_AllReasons_PublishSuccessfully(DeadLetterReason reason)
    {
        var publisher = BuildPublisher();
        var envelope = BuildEnvelope();
        IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await publisher.PublishAsync(envelope, reason, "error", 1, CancellationToken.None);

        Assert.That(captured!.Payload.Reason, Is.EqualTo(reason));
    }
}
