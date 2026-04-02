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
}
