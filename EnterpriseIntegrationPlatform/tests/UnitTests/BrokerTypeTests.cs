using EnterpriseIntegrationPlatform.Ingestion;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class BrokerTypeTests
{
    [Fact]
    public void BrokerType_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<BrokerType>().Should().HaveCount(3);
        ((int)BrokerType.NatsJetStream).Should().Be(0);
        ((int)BrokerType.Kafka).Should().Be(1);
        ((int)BrokerType.Pulsar).Should().Be(2);
    }

    [Theory]
    [InlineData("NatsJetStream", BrokerType.NatsJetStream)]
    [InlineData("Kafka", BrokerType.Kafka)]
    [InlineData("Pulsar", BrokerType.Pulsar)]
    public void BrokerType_ParsesFromString(string input, BrokerType expected)
    {
        // Act
        var parsed = Enum.Parse<BrokerType>(input);

        // Assert
        parsed.Should().Be(expected);
    }
}
