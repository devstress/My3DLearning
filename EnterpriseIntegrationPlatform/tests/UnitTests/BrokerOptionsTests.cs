using EnterpriseIntegrationPlatform.Ingestion;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class BrokerOptionsTests
{
    [Test]
    public void DefaultBrokerType_IsNatsJetStream()
    {
        // Arrange & Act
        var options = new BrokerOptions();

        // Assert
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.NatsJetStream));
    }

    [Test]
    public void DefaultConnectionString_IsEmpty()
    {
        // Arrange & Act
        var options = new BrokerOptions();

        // Assert
        Assert.That(options.ConnectionString, Is.Empty);
    }

    [Test]
    public void SectionName_IsBroker()
    {
        // Assert
        Assert.That(BrokerOptions.SectionName, Is.EqualTo("Broker"));
    }

    [Test]
    public void BrokerType_CanBeSetToKafka()
    {
        // Arrange & Act
        var options = new BrokerOptions { BrokerType = BrokerType.Kafka };

        // Assert
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.Kafka));
    }

    [Test]
    public void BrokerType_CanBeSetToPulsar()
    {
        // Arrange & Act
        var options = new BrokerOptions { BrokerType = BrokerType.Pulsar };

        // Assert
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.Pulsar));
    }

    [Test]
    public void ConnectionString_CanBeSet()
    {
        // Arrange & Act
        var options = new BrokerOptions
        {
            ConnectionString = "nats://localhost:4222",
        };

        // Assert
        Assert.That(options.ConnectionString, Is.EqualTo("nats://localhost:4222"));
    }

    [Test]
    public void BrokerType_CanBeSetToNorthguard()
    {
        // Arrange & Act
        var options = new BrokerOptions { BrokerType = BrokerType.Northguard };

        // Assert
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.Northguard));
    }
}
