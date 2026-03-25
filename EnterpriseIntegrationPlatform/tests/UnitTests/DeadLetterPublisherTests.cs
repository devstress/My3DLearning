using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

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

    [Fact]
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

        capturedTopic.Should().Be("dlq.orders");
    }

    [Fact]
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

        captured!.Payload.OriginalEnvelope.Payload.Should().Be("original-payload");
    }

    [Fact]
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

        captured!.CorrelationId.Should().Be(envelope.CorrelationId);
    }

    [Fact]
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

        captured!.CausationId.Should().Be(envelope.MessageId);
    }

    [Fact]
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

        captured!.MessageId.Should().NotBe(envelope.MessageId);
        captured!.MessageId.Should().NotBe(Guid.Empty);
    }

    [Fact]
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

        captured!.Source.Should().Be("OverrideSource");
    }

    [Fact]
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

        captured!.MessageType.Should().Be("CustomDLQ");
    }

    [Fact]
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

        captured!.Payload.Reason.Should().Be(DeadLetterReason.ValidationFailed);
        captured!.Payload.ErrorMessage.Should().Be("validation error");
    }

    [Fact]
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

        captured!.Payload.FailedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
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

        captured!.Payload.AttemptCount.Should().Be(5);
    }

    [Fact]
    public async Task PublishAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var publisher = BuildPublisher();

        var act = async () => await publisher.PublishAsync(null!, DeadLetterReason.PoisonMessage, "error", 1, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishAsync_EmptyDeadLetterTopic_ThrowsInvalidOperationException()
    {
        var publisher = BuildPublisher(new DeadLetterOptions { DeadLetterTopic = "" });
        var envelope = BuildEnvelope();

        var act = async () => await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage, "error", 1, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(DeadLetterReason.MaxRetriesExceeded)]
    [InlineData(DeadLetterReason.PoisonMessage)]
    [InlineData(DeadLetterReason.ProcessingTimeout)]
    [InlineData(DeadLetterReason.ValidationFailed)]
    [InlineData(DeadLetterReason.UnroutableMessage)]
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

        captured!.Payload.Reason.Should().Be(reason);
    }
}
