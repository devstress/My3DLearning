using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class InvalidMessageChannelTests
{
    private IMessageBrokerProducer _producer = null!;
    private ILogger<InvalidMessageChannel> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _logger = Substitute.For<ILogger<InvalidMessageChannel>>();
    }

    private InvalidMessageChannel BuildChannel(InvalidMessageChannelOptions? options = null)
    {
        options ??= new InvalidMessageChannelOptions();
        return new InvalidMessageChannel(_producer, Options.Create(options), _logger);
    }

    private static IntegrationEnvelope<string> BuildEnvelope(string payload = "bad-data")
        => IntegrationEnvelope<string>.Create(payload, "TestService", "TestEvent");

    [Test]
    public async Task RouteInvalidAsync_ValidEnvelope_PublishesToInvalidTopic()
    {
        var channel = BuildChannel();
        var envelope = BuildEnvelope();
        string? capturedTopic = null;
        await _producer.PublishAsync(
            Arg.Any<IntegrationEnvelope<InvalidMessageEnvelope>>(),
            Arg.Do<string>(t => capturedTopic = t),
            Arg.Any<CancellationToken>());

        await channel.RouteInvalidAsync(envelope, "schema validation failed", CancellationToken.None);

        Assert.That(capturedTopic, Is.EqualTo("invalid-messages"));
    }

    [Test]
    public async Task RouteInvalidAsync_CustomTopic_UsesConfiguredTopic()
    {
        var channel = BuildChannel(new InvalidMessageChannelOptions { InvalidMessageTopic = "custom.invalid" });
        var envelope = BuildEnvelope();
        string? capturedTopic = null;
        await _producer.PublishAsync(
            Arg.Any<IntegrationEnvelope<InvalidMessageEnvelope>>(),
            Arg.Do<string>(t => capturedTopic = t),
            Arg.Any<CancellationToken>());

        await channel.RouteInvalidAsync(envelope, "bad format", CancellationToken.None);

        Assert.That(capturedTopic, Is.EqualTo("custom.invalid"));
    }

    [Test]
    public async Task RouteInvalidAsync_ValidEnvelope_PreservesCorrelationId()
    {
        var channel = BuildChannel();
        var envelope = BuildEnvelope();
        IntegrationEnvelope<InvalidMessageEnvelope>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<InvalidMessageEnvelope>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await channel.RouteInvalidAsync(envelope, "invalid", CancellationToken.None);

        Assert.That(captured!.CorrelationId, Is.EqualTo(envelope.CorrelationId));
    }

    [Test]
    public async Task RouteInvalidAsync_ValidEnvelope_SetsCausationId()
    {
        var channel = BuildChannel();
        var envelope = BuildEnvelope();
        IntegrationEnvelope<InvalidMessageEnvelope>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<InvalidMessageEnvelope>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await channel.RouteInvalidAsync(envelope, "invalid", CancellationToken.None);

        Assert.That(captured!.CausationId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task RouteInvalidAsync_ValidEnvelope_CapturesReason()
    {
        var channel = BuildChannel();
        var envelope = BuildEnvelope();
        IntegrationEnvelope<InvalidMessageEnvelope>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<InvalidMessageEnvelope>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await channel.RouteInvalidAsync(envelope, "XML schema mismatch", CancellationToken.None);

        Assert.That(captured!.Payload.Reason, Is.EqualTo("XML schema mismatch"));
    }

    [Test]
    public async Task RouteInvalidAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var channel = BuildChannel();

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await channel.RouteInvalidAsync<string>(null!, "reason", CancellationToken.None));
    }

    [Test]
    public async Task RouteInvalidAsync_EmptyReason_ThrowsArgumentException()
    {
        var channel = BuildChannel();
        var envelope = BuildEnvelope();

        Assert.ThrowsAsync<ArgumentException>(
            async () => await channel.RouteInvalidAsync(envelope, "", CancellationToken.None));
    }

    [Test]
    public async Task RouteInvalidAsync_EmptyTopic_ThrowsInvalidOperationException()
    {
        var channel = BuildChannel(new InvalidMessageChannelOptions { InvalidMessageTopic = "" });
        var envelope = BuildEnvelope();

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await channel.RouteInvalidAsync(envelope, "reason", CancellationToken.None));
    }

    [Test]
    public async Task RouteRawInvalidAsync_ValidData_PublishesToInvalidTopic()
    {
        var channel = BuildChannel();
        string? capturedTopic = null;
        await _producer.PublishAsync(
            Arg.Any<IntegrationEnvelope<InvalidMessageEnvelope>>(),
            Arg.Do<string>(t => capturedTopic = t),
            Arg.Any<CancellationToken>());

        await channel.RouteRawInvalidAsync("{broken-json", "orders.inbound", "parse failure", CancellationToken.None);

        Assert.That(capturedTopic, Is.EqualTo("invalid-messages"));
    }

    [Test]
    public async Task RouteRawInvalidAsync_ValidData_CapturesRawDataAndReason()
    {
        var channel = BuildChannel();
        IntegrationEnvelope<InvalidMessageEnvelope>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<InvalidMessageEnvelope>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await channel.RouteRawInvalidAsync("<xml>bad</xml>", "inbound", "not JSON", CancellationToken.None);

        Assert.That(captured!.Payload.RawData, Is.EqualTo("<xml>bad</xml>"));
        Assert.That(captured!.Payload.Reason, Is.EqualTo("not JSON"));
        Assert.That(captured!.Payload.SourceTopic, Is.EqualTo("inbound"));
    }

    [Test]
    public async Task RouteRawInvalidAsync_ValidData_SetsOriginalMessageIdToEmpty()
    {
        var channel = BuildChannel();
        IntegrationEnvelope<InvalidMessageEnvelope>? captured = null;
        await _producer.PublishAsync(
            Arg.Do<IntegrationEnvelope<InvalidMessageEnvelope>>(e => captured = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await channel.RouteRawInvalidAsync("raw", "topic", "reason", CancellationToken.None);

        Assert.That(captured!.Payload.OriginalMessageId, Is.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task RouteRawInvalidAsync_EmptyRawData_ThrowsArgumentException()
    {
        var channel = BuildChannel();

        Assert.ThrowsAsync<ArgumentException>(
            async () => await channel.RouteRawInvalidAsync("", "topic", "reason", CancellationToken.None));
    }

    [Test]
    public async Task RouteRawInvalidAsync_EmptySourceTopic_ThrowsArgumentException()
    {
        var channel = BuildChannel();

        Assert.ThrowsAsync<ArgumentException>(
            async () => await channel.RouteRawInvalidAsync("data", "", "reason", CancellationToken.None));
    }
}
