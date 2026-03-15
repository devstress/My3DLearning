using EnterpriseIntegrationPlatform.Ingestion;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class BrokerOptionsTests
{
    [Fact]
    public void DefaultBrokerType_IsNatsJetStream()
    {
        // Arrange & Act
        var options = new BrokerOptions();

        // Assert
        options.BrokerType.Should().Be(BrokerType.NatsJetStream);
    }

    [Fact]
    public void DefaultConnectionString_IsEmpty()
    {
        // Arrange & Act
        var options = new BrokerOptions();

        // Assert
        options.ConnectionString.Should().BeEmpty();
    }

    [Fact]
    public void SectionName_IsBroker()
    {
        // Assert
        BrokerOptions.SectionName.Should().Be("Broker");
    }

    [Fact]
    public void BrokerType_CanBeSetToKafka()
    {
        // Arrange & Act
        var options = new BrokerOptions { BrokerType = BrokerType.Kafka };

        // Assert
        options.BrokerType.Should().Be(BrokerType.Kafka);
    }

    [Fact]
    public void BrokerType_CanBeSetToPulsar()
    {
        // Arrange & Act
        var options = new BrokerOptions { BrokerType = BrokerType.Pulsar };

        // Assert
        options.BrokerType.Should().Be(BrokerType.Pulsar);
    }

    [Fact]
    public void ConnectionString_CanBeSet()
    {
        // Arrange & Act
        var options = new BrokerOptions
        {
            ConnectionString = "nats://localhost:4222",
        };

        // Assert
        options.ConnectionString.Should().Be("nats://localhost:4222");
    }
}
