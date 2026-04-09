using DotPulsar.Abstractions;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Pulsar;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PulsarProducerTests
{
    // ------------------------------------------------------------------ //
    // Constructor validation
    // ------------------------------------------------------------------ //

    [Test]
    public void Constructor_NullClient_Throws()
    {
        Assert.That(
            () => new PulsarProducer(null!, NullLogger<PulsarProducer>.Instance),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        Assert.That(
            () => new PulsarProducer(client, null!),
            Throws.ArgumentNullException);
    }

    // ------------------------------------------------------------------ //
    // PublishAsync – argument validation
    // ------------------------------------------------------------------ //

    [Test]
    public void PublishAsync_NullEnvelope_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarProducer(client, NullLogger<PulsarProducer>.Instance);

        Assert.That(
            async () => await sut.PublishAsync<string>(null!, "my-topic"),
            Throws.ArgumentNullException);
    }

    [Test]
    public void PublishAsync_EmptyTopic_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarProducer(client, NullLogger<PulsarProducer>.Instance);
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "type");

        Assert.That(
            async () => await sut.PublishAsync(envelope, ""),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void PublishAsync_NullTopic_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarProducer(client, NullLogger<PulsarProducer>.Instance);
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "type");

        Assert.That(
            async () => await sut.PublishAsync(envelope, null!),
            Throws.InstanceOf<ArgumentException>());
    }

    // ------------------------------------------------------------------ //
    // DisposeAsync — cleans up cached producers
    // ------------------------------------------------------------------ //

    [Test]
    public async Task DisposeAsync_CanBeCalledSafely()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarProducer(client, NullLogger<PulsarProducer>.Instance);

        await sut.DisposeAsync();

        Assert.Pass();
    }

    [Test]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarProducer(client, NullLogger<PulsarProducer>.Instance);

        await sut.DisposeAsync();
        await sut.DisposeAsync();

        Assert.Pass();
    }
}
