using DotPulsar.Abstractions;
using EnterpriseIntegrationPlatform.Ingestion.Pulsar;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PulsarProducerCachingTests
{
    // ------------------------------------------------------------------ //
    // Constructor and lifecycle
    // ------------------------------------------------------------------ //

    [Test]
    public void Constructor_ValidArgs_CachedProducerCountIsZero()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarProducer(client, NullLogger<PulsarProducer>.Instance);

        Assert.That(sut.CachedProducerCount, Is.EqualTo(0));
    }

    [Test]
    public async Task DisposeAsync_EmptyCache_DoesNotThrow()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarProducer(client, NullLogger<PulsarProducer>.Instance);

        await sut.DisposeAsync();

        Assert.That(sut.CachedProducerCount, Is.EqualTo(0));
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

    // ------------------------------------------------------------------ //
    // ObjectDisposedException after dispose
    // ------------------------------------------------------------------ //

    [Test]
    public async Task PublishAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarProducer(client, NullLogger<PulsarProducer>.Instance);
        await sut.DisposeAsync();

        var envelope = EnterpriseIntegrationPlatform.Contracts.IntegrationEnvelope<string>.Create("payload", "src", "type");
        Assert.That(
            async () => await sut.PublishAsync(envelope, "topic"),
            Throws.InstanceOf<ObjectDisposedException>());
    }

    // ------------------------------------------------------------------ //
    // Argument validation
    // ------------------------------------------------------------------ //

    [Test]
    public void PublishAsync_NullEnvelope_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarProducer(client, NullLogger<PulsarProducer>.Instance);

        Assert.That(
            async () => await sut.PublishAsync<string>(null!, "topic"),
            Throws.ArgumentNullException);
    }

    [Test]
    public void PublishAsync_EmptyTopic_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarProducer(client, NullLogger<PulsarProducer>.Instance);
        var envelope = EnterpriseIntegrationPlatform.Contracts.IntegrationEnvelope<string>.Create("p", "src", "type");

        Assert.That(
            async () => await sut.PublishAsync(envelope, ""),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void PublishAsync_WhitespaceTopic_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarProducer(client, NullLogger<PulsarProducer>.Instance);
        var envelope = EnterpriseIntegrationPlatform.Contracts.IntegrationEnvelope<string>.Create("p", "src", "type");

        Assert.That(
            async () => await sut.PublishAsync(envelope, "   "),
            Throws.InstanceOf<ArgumentException>());
    }
}
