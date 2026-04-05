using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Kafka;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class KafkaServiceExtensionsTests
{
    // ------------------------------------------------------------------ //
    // Registration
    // ------------------------------------------------------------------ //

    [Test]
    public async Task AddKafkaBroker_RegistersIMessageBrokerProducer()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddKafkaBroker("localhost:9092");

        await using var provider = services.BuildServiceProvider();
        var producer = provider.GetService<IMessageBrokerProducer>();

        Assert.That(producer, Is.Not.Null);
        Assert.That(producer, Is.InstanceOf<KafkaProducer>());
    }

    [Test]
    public async Task AddKafkaBroker_RegistersIMessageBrokerConsumer()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddKafkaBroker("localhost:9092");

        await using var provider = services.BuildServiceProvider();
        var consumer = provider.GetService<IMessageBrokerConsumer>();

        Assert.That(consumer, Is.Not.Null);
        Assert.That(consumer, Is.InstanceOf<KafkaConsumer>());
    }

    // ------------------------------------------------------------------ //
    // Argument validation
    // ------------------------------------------------------------------ //

    [Test]
    public void AddKafkaBroker_NullBootstrapServers_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Assert.That(
            () => services.AddKafkaBroker(null!),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void AddKafkaBroker_EmptyBootstrapServers_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Assert.That(
            () => services.AddKafkaBroker(""),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void AddKafkaBroker_WhitespaceBootstrapServers_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Assert.That(
            () => services.AddKafkaBroker("   "),
            Throws.InstanceOf<ArgumentException>());
    }
}
