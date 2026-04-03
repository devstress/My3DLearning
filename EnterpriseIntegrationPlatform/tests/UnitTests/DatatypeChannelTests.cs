using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class DatatypeChannelTests
{
    private IMessageBrokerProducer _producer = null!;
    private ILogger<DatatypeChannel> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _logger = Substitute.For<ILogger<DatatypeChannel>>();
    }

    private DatatypeChannel BuildChannel(DatatypeChannelOptions? options = null)
    {
        options ??= new DatatypeChannelOptions();
        return new DatatypeChannel(_producer, Options.Create(options), _logger);
    }

    private static IntegrationEnvelope<string> BuildEnvelope(string messageType = "OrderCreated")
        => IntegrationEnvelope<string>.Create("payload", "TestService", messageType);

    [Test]
    public async Task PublishAsync_ValidEnvelope_PublishesToDerivedTopic()
    {
        var channel = BuildChannel();
        var envelope = BuildEnvelope("OrderCreated");

        await channel.PublishAsync(envelope, CancellationToken.None);

        await _producer.Received(1).PublishAsync(
            envelope, "datatype.ordercreated", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAsync_CustomPrefix_UsesCustomPrefixInTopic()
    {
        var channel = BuildChannel(new DatatypeChannelOptions { TopicPrefix = "eip" });
        var envelope = BuildEnvelope("InvoicePaid");

        await channel.PublishAsync(envelope, CancellationToken.None);

        await _producer.Received(1).PublishAsync(
            envelope, "eip.invoicepaid", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAsync_EmptyPrefix_UsesMessageTypeOnly()
    {
        var channel = BuildChannel(new DatatypeChannelOptions { TopicPrefix = "" });
        var envelope = BuildEnvelope("ShipmentReady");

        await channel.PublishAsync(envelope, CancellationToken.None);

        await _producer.Received(1).PublishAsync(
            envelope, "shipmentready", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAsync_CustomSeparator_UsesCustomSeparator()
    {
        var channel = BuildChannel(new DatatypeChannelOptions { Separator = "/" });
        var envelope = BuildEnvelope("UserRegistered");

        await channel.PublishAsync(envelope, CancellationToken.None);

        await _producer.Received(1).PublishAsync(
            envelope, "datatype/userregistered", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var channel = BuildChannel();

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await channel.PublishAsync<string>(null!, CancellationToken.None));
    }

    [Test]
    public void ResolveChannel_ValidMessageType_ReturnsResolvedTopic()
    {
        var channel = BuildChannel();

        var result = channel.ResolveChannel("OrderCreated");

        Assert.That(result, Is.EqualTo("datatype.ordercreated"));
    }

    [Test]
    public void ResolveChannel_EmptyMessageType_ThrowsArgumentException()
    {
        var channel = BuildChannel();

        Assert.Throws<ArgumentException>(() => channel.ResolveChannel(""));
    }

    [Test]
    public async Task PublishAsync_EmptyMessageType_ThrowsInvalidOperationException()
    {
        var channel = BuildChannel();
        var envelope = new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "TestService",
            MessageType = "",
            Payload = "test",
        };

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await channel.PublishAsync(envelope, CancellationToken.None));
    }
}
