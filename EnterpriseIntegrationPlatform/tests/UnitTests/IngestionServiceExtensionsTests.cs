using EnterpriseIntegrationPlatform.Ingestion;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class IngestionServiceExtensionsTests
{
    [Fact]
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
        options.BrokerType.Should().Be(BrokerType.Kafka);
        options.ConnectionString.Should().Be("localhost:9092");
    }

    [Fact]
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
        options.BrokerType.Should().Be(BrokerType.NatsJetStream);
    }

    [Fact]
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
        options.BrokerType.Should().Be(BrokerType.Pulsar);
        options.ConnectionString.Should().Be("pulsar://localhost:6650");
    }
}
