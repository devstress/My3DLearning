using Confluent.Kafka;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class KafkaProducerTests
{
    private IProducer<string, byte[]> _innerProducer = null!;
    private KafkaProducer _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _innerProducer = Substitute.For<IProducer<string, byte[]>>();
        _sut = new KafkaProducer(_innerProducer, NullLogger<KafkaProducer>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
        _innerProducer.Dispose();
    }

    // ------------------------------------------------------------------ //
    // Constructor validation
    // ------------------------------------------------------------------ //

    [Test]
    public void Constructor_NullProducer_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new KafkaProducer(null!, NullLogger<KafkaProducer>.Instance),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var producer = Substitute.For<IProducer<string, byte[]>>();
        Assert.That(
            () => new KafkaProducer(producer, null!),
            Throws.ArgumentNullException);
    }

    // ------------------------------------------------------------------ //
    // PublishAsync – happy path
    // ------------------------------------------------------------------ //

    [Test]
    public async Task PublishAsync_ValidEnvelope_ProducesToCorrectTopic()
    {
        var envelope = IntegrationEnvelope<string>.Create("test-payload", "unit-test", "TestMessage");

        await _sut.PublishAsync(envelope, "my-topic");

        await _innerProducer.Received(1).ProduceAsync(
            "my-topic",
            Arg.Any<Message<string, byte[]>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAsync_ValidEnvelope_SerializesPayloadToBytes()
    {
        var envelope = IntegrationEnvelope<string>.Create("test-payload", "unit-test", "TestMessage");
        var expected = EnvelopeSerializer.Serialize(envelope);

        await _sut.PublishAsync(envelope, "my-topic");

        await _innerProducer.Received(1).ProduceAsync(
            Arg.Any<string>(),
            Arg.Is<Message<string, byte[]>>(m => m.Value.Length == expected.Length),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAsync_ValidEnvelope_UsesCorrelationIdAsKey()
    {
        var envelope = IntegrationEnvelope<string>.Create("test-payload", "unit-test", "TestMessage");
        var expectedKey = envelope.CorrelationId.ToString();

        await _sut.PublishAsync(envelope, "my-topic");

        await _innerProducer.Received(1).ProduceAsync(
            Arg.Any<string>(),
            Arg.Is<Message<string, byte[]>>(m => m.Key == expectedKey),
            Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ //
    // PublishAsync – argument validation
    // ------------------------------------------------------------------ //

    [Test]
    public void PublishAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        Assert.That(
            async () => await _sut.PublishAsync<string>(null!, "my-topic"),
            Throws.ArgumentNullException);
    }

    [Test]
    public void PublishAsync_NullTopic_ThrowsArgumentException()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "type");
        Assert.That(
            async () => await _sut.PublishAsync(envelope, null!),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void PublishAsync_EmptyTopic_ThrowsArgumentException()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "type");
        Assert.That(
            async () => await _sut.PublishAsync(envelope, ""),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void PublishAsync_WhitespaceTopic_ThrowsArgumentException()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "type");
        Assert.That(
            async () => await _sut.PublishAsync(envelope, "   "),
            Throws.InstanceOf<ArgumentException>());
    }

    // ------------------------------------------------------------------ //
    // Dispose
    // ------------------------------------------------------------------ //

    [Test]
    public void Dispose_DisposesUnderlyingProducer()
    {
        var inner = Substitute.For<IProducer<string, byte[]>>();
        var producer = new KafkaProducer(inner, NullLogger<KafkaProducer>.Instance);

        producer.Dispose();

        inner.Received(1).Dispose();
    }
}
