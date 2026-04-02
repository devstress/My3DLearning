using EnterpriseIntegrationPlatform.Ingestion;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class BrokerTypeTests
{
    [Test]
    public void BrokerType_HasExpectedValues()
    {
        // Assert
        Assert.That(Enum.GetValues<BrokerType>().Length, Is.EqualTo(3));
        Assert.That(((int)BrokerType.NatsJetStream), Is.EqualTo(0));
        Assert.That(((int)BrokerType.Kafka), Is.EqualTo(1));
        Assert.That(((int)BrokerType.Pulsar), Is.EqualTo(2));
    }

    [TestCase("NatsJetStream", BrokerType.NatsJetStream)]
    [TestCase("Kafka", BrokerType.Kafka)]
    [TestCase("Pulsar", BrokerType.Pulsar)]
    public void BrokerType_ParsesFromString(string input, BrokerType expected)
    {
        // Act
        var parsed = Enum.Parse<BrokerType>(input);

        // Assert
        Assert.That(parsed, Is.EqualTo(expected));
    }
}
