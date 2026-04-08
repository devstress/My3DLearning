using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Nats;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class NatsServiceExtensionsFullTests
{
    [Test]
    public async Task AddNatsJetStreamBroker_RegistersProducerAndConsumer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNatsJetStreamBroker("nats://localhost:4222");

        await using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            var producer = provider.GetService<IMessageBrokerProducer>();
            Assert.That(producer, Is.Not.Null);
            Assert.That(producer, Is.InstanceOf<NatsJetStreamProducer>());

            var consumer = provider.GetService<IMessageBrokerConsumer>();
            Assert.That(consumer, Is.Not.Null);
            Assert.That(consumer, Is.InstanceOf<NatsJetStreamConsumer>());
        });
    }

    [Test]
    public async Task AddNatsJetStreamBroker_RegistersNatsOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNatsJetStreamBroker("nats://custom-host:5222");

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NatsOptions>>().Value;

        Assert.That(options.Url, Is.EqualTo("nats://custom-host:5222"));
    }

    [Test]
    public async Task AddNatsJetStreamBroker_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNatsJetStreamBroker("nats://localhost:4222");

        await using var provider = services.BuildServiceProvider();
        var healthCheck = provider.GetService<NatsHealthCheck>();

        Assert.That(healthCheck, Is.Not.Null);
    }

    [Test]
    public void AddNatsJetStreamBroker_NullConnectionString_Throws()
    {
        var services = new ServiceCollection();

        Assert.That(
            () => services.AddNatsJetStreamBroker(null!),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void AddNatsJetStreamBroker_EmptyConnectionString_Throws()
    {
        var services = new ServiceCollection();

        Assert.That(
            () => services.AddNatsJetStreamBroker(""),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void AddNatsJetStreamBroker_WhitespaceConnectionString_Throws()
    {
        var services = new ServiceCollection();

        Assert.That(
            () => services.AddNatsJetStreamBroker("   "),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public async Task AddNatsJetStreamBroker_OptionsRetainDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNatsJetStreamBroker("nats://localhost:4222");

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NatsOptions>>().Value;

        Assert.Multiple(() =>
        {
            Assert.That(options.MaxRetries, Is.EqualTo(3));
            Assert.That(options.RetryDelayMs, Is.EqualTo(1000));
        });
    }
}
