using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Nats;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

/// <summary>
/// Tests for <see cref="NatsJetStreamConsumer"/> type contracts.
/// </summary>
[TestFixture]
public class NatsConsumerTests
{
    [Test]
    public void NatsJetStreamConsumer_ImplementsIMessageBrokerConsumer()
    {
        Assert.That(typeof(IMessageBrokerConsumer).IsAssignableFrom(typeof(NatsJetStreamConsumer)), Is.True);
    }

    [Test]
    public void NatsJetStreamConsumer_IsSealed()
    {
        Assert.That(typeof(NatsJetStreamConsumer).IsSealed, Is.True);
    }
}
