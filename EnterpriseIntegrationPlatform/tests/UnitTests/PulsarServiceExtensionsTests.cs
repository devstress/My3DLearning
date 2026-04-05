using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Pulsar;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PulsarServiceExtensionsTests
{
    // ------------------------------------------------------------------ //
    // Registration
    // ------------------------------------------------------------------ //

    [Test]
    public async Task AddPulsarBroker_Registers_Producer()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddPulsarBroker("pulsar://localhost:6650");

        await using var provider = services.BuildServiceProvider();
        var producer = provider.GetService<IMessageBrokerProducer>();

        Assert.That(producer, Is.Not.Null);
        Assert.That(producer, Is.InstanceOf<PulsarProducer>());
    }

    [Test]
    public async Task AddPulsarBroker_Registers_Consumer()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddPulsarBroker("pulsar://localhost:6650");

        await using var provider = services.BuildServiceProvider();
        var consumer = provider.GetService<IMessageBrokerConsumer>();

        Assert.That(consumer, Is.Not.Null);
        Assert.That(consumer, Is.InstanceOf<PulsarConsumer>());
    }

    // ------------------------------------------------------------------ //
    // Argument validation
    // ------------------------------------------------------------------ //

    [Test]
    public void AddPulsarBroker_NullServiceUrl_Throws()
    {
        var services = new ServiceCollection();

        Assert.That(
            () => services.AddPulsarBroker(null!),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void AddPulsarBroker_EmptyServiceUrl_Throws()
    {
        var services = new ServiceCollection();

        Assert.That(
            () => services.AddPulsarBroker(""),
            Throws.InstanceOf<ArgumentException>());
    }
}
