using Confluent.Kafka;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class KafkaConsumerTests
{
    private static readonly ConsumerConfig DefaultConfig = new()
    {
        BootstrapServers = "localhost:9092",
    };

    // ------------------------------------------------------------------ //
    // Constructor validation
    // ------------------------------------------------------------------ //

    [Test]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new KafkaConsumer(null!, NullLogger<KafkaConsumer>.Instance),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new KafkaConsumer(DefaultConfig, null!),
            Throws.ArgumentNullException);
    }

    // ------------------------------------------------------------------ //
    // SubscribeAsync – argument validation
    // ------------------------------------------------------------------ //

    [Test]
    public void SubscribeAsync_NullTopic_ThrowsArgumentException()
    {
        var sut = new KafkaConsumer(DefaultConfig, NullLogger<KafkaConsumer>.Instance);

        Assert.That(
            async () => await sut.SubscribeAsync<string>(
                null!, "group", _ => Task.CompletedTask),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void SubscribeAsync_EmptyTopic_ThrowsArgumentException()
    {
        var sut = new KafkaConsumer(DefaultConfig, NullLogger<KafkaConsumer>.Instance);

        Assert.That(
            async () => await sut.SubscribeAsync<string>(
                "", "group", _ => Task.CompletedTask),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void SubscribeAsync_NullConsumerGroup_ThrowsArgumentException()
    {
        var sut = new KafkaConsumer(DefaultConfig, NullLogger<KafkaConsumer>.Instance);

        Assert.That(
            async () => await sut.SubscribeAsync<string>(
                "topic", null!, _ => Task.CompletedTask),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void SubscribeAsync_EmptyConsumerGroup_ThrowsArgumentException()
    {
        var sut = new KafkaConsumer(DefaultConfig, NullLogger<KafkaConsumer>.Instance);

        Assert.That(
            async () => await sut.SubscribeAsync<string>(
                "topic", "", _ => Task.CompletedTask),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void SubscribeAsync_NullHandler_ThrowsArgumentNullException()
    {
        var sut = new KafkaConsumer(DefaultConfig, NullLogger<KafkaConsumer>.Instance);

        Assert.That(
            async () => await sut.SubscribeAsync<string>(
                "topic", "group", null!),
            Throws.ArgumentNullException);
    }

    // ------------------------------------------------------------------ //
    // DisposeAsync
    // ------------------------------------------------------------------ //

    [Test]
    public async Task DisposeAsync_ReturnsCompletedValueTask()
    {
        var sut = new KafkaConsumer(DefaultConfig, NullLogger<KafkaConsumer>.Instance);

        var task = sut.DisposeAsync();

        Assert.That(task.IsCompleted, Is.True);
        await task;
    }
}
