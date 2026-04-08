using DotPulsar.Abstractions;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Pulsar;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PulsarConsumerTests
{
    // ------------------------------------------------------------------ //
    // Constructor validation
    // ------------------------------------------------------------------ //

    [Test]
    public void Constructor_NullClient_Throws()
    {
        Assert.That(
            () => new PulsarConsumer(null!, NullLogger<PulsarConsumer>.Instance),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        Assert.That(
            () => new PulsarConsumer(client, null!),
            Throws.ArgumentNullException);
    }

    // ------------------------------------------------------------------ //
    // SubscribeAsync – argument validation
    // ------------------------------------------------------------------ //

    [Test]
    public void SubscribeAsync_NullTopic_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarConsumer(client, NullLogger<PulsarConsumer>.Instance);

        Assert.That(
            async () => await sut.SubscribeAsync<string>(
                null!, "group", _ => Task.CompletedTask),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void SubscribeAsync_EmptyTopic_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarConsumer(client, NullLogger<PulsarConsumer>.Instance);

        Assert.That(
            async () => await sut.SubscribeAsync<string>(
                "", "group", _ => Task.CompletedTask),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void SubscribeAsync_NullConsumerGroup_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarConsumer(client, NullLogger<PulsarConsumer>.Instance);

        Assert.That(
            async () => await sut.SubscribeAsync<string>(
                "topic", null!, _ => Task.CompletedTask),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void SubscribeAsync_EmptyConsumerGroup_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarConsumer(client, NullLogger<PulsarConsumer>.Instance);

        Assert.That(
            async () => await sut.SubscribeAsync<string>(
                "topic", "", _ => Task.CompletedTask),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void SubscribeAsync_NullHandler_Throws()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarConsumer(client, NullLogger<PulsarConsumer>.Instance);

        Assert.That(
            async () => await sut.SubscribeAsync<string>(
                "topic", "group", null!),
            Throws.ArgumentNullException);
    }

    // ------------------------------------------------------------------ //
    // DisposeAsync — cancels and cleans up
    // ------------------------------------------------------------------ //

    [Test]
    public async Task DisposeAsync_CanBeCalledSafely()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarConsumer(client, NullLogger<PulsarConsumer>.Instance);

        await sut.DisposeAsync();

        Assert.Pass();
    }

    [Test]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarConsumer(client, NullLogger<PulsarConsumer>.Instance);

        await sut.DisposeAsync();
        await sut.DisposeAsync();

        Assert.Pass();
    }

    [Test]
    public async Task SubscribeAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarConsumer(client, NullLogger<PulsarConsumer>.Instance);
        await sut.DisposeAsync();

        Assert.That(
            async () => await sut.SubscribeAsync<string>(
                "topic", "group", _ => Task.CompletedTask),
            Throws.InstanceOf<ObjectDisposedException>());
    }
}
