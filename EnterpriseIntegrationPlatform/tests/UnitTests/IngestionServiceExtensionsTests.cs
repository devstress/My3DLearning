using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class IngestionServiceExtensionsTests
{
    [Test]
    public void AddBrokerOptions_BindsFromConfiguration()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Broker:BrokerType"] = "Kafka",
                ["Broker:ConnectionString"] = "localhost:9092",
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddBrokerOptions(config);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BrokerOptions>>().Value;

        // Assert
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.Kafka));
        Assert.That(options.ConnectionString, Is.EqualTo("localhost:9092"));
    }

    [Test]
    public void AddBrokerOptions_DefaultsToNatsJetStream_WhenNotConfigured()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddBrokerOptions(config);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BrokerOptions>>().Value;

        // Assert
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.NatsJetStream));
    }

    [Test]
    public void AddBrokerOptions_BindsPulsarType()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Broker:BrokerType"] = "Pulsar",
                ["Broker:ConnectionString"] = "pulsar://localhost:6650",
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddBrokerOptions(config);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BrokerOptions>>().Value;

        // Assert
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.Pulsar));
        Assert.That(options.ConnectionString, Is.EqualTo("pulsar://localhost:6650"));
    }

    [Test]
    public void AddIngestion_NatsJetStream_RegistersProducerAndConsumer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddIngestion(options =>
        {
            options.BrokerType = BrokerType.NatsJetStream;
            options.ConnectionString = "nats://localhost:15222";
        });

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<BrokerOptions>>().Value;

        Assert.That(opts.BrokerType, Is.EqualTo(BrokerType.NatsJetStream));
        Assert.That(opts.ConnectionString, Is.EqualTo("nats://localhost:15222"));

        // Verify that broker-specific registrations occurred
        Assert.That(services.Any(sd => sd.ServiceType == typeof(IMessageBrokerProducer)), Is.True);
        Assert.That(services.Any(sd => sd.ServiceType == typeof(IMessageBrokerConsumer)), Is.True);
    }

    [Test]
    public void AddIngestion_Kafka_RegistersProducerAndConsumer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddIngestion(options =>
        {
            options.BrokerType = BrokerType.Kafka;
            options.ConnectionString = "localhost:9092";
        });

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<BrokerOptions>>().Value;

        Assert.That(opts.BrokerType, Is.EqualTo(BrokerType.Kafka));
        Assert.That(services.Any(sd => sd.ServiceType == typeof(IMessageBrokerProducer)), Is.True);
        Assert.That(services.Any(sd => sd.ServiceType == typeof(IMessageBrokerConsumer)), Is.True);
    }

    [Test]
    public void AddIngestion_Pulsar_RegistersProducerAndConsumer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddIngestion(options =>
        {
            options.BrokerType = BrokerType.Pulsar;
            options.ConnectionString = "pulsar://localhost:6650";
        });

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<BrokerOptions>>().Value;

        Assert.That(opts.BrokerType, Is.EqualTo(BrokerType.Pulsar));
        Assert.That(services.Any(sd => sd.ServiceType == typeof(IMessageBrokerProducer)), Is.True);
        Assert.That(services.Any(sd => sd.ServiceType == typeof(IMessageBrokerConsumer)), Is.True);
    }

    [Test]
    public void AddIngestion_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddIngestion(null!));
    }
}
