using System.Diagnostics;
using EnterpriseIntegrationPlatform.Ingestion.Nats;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

/// <summary>
/// Tests for <see cref="NatsJetStreamProducer"/> diagnostics and type contracts.
/// </summary>
[TestFixture]
public class NatsProducerTests
{
    [Test]
    public void NatsJetStreamProducer_ImplementsIAsyncDisposable()
    {
        Assert.That(typeof(IAsyncDisposable).IsAssignableFrom(typeof(NatsJetStreamProducer)), Is.True);
    }

    [Test]
    public void NatsJetStreamProducer_IsSealed()
    {
        Assert.That(typeof(NatsJetStreamProducer).IsSealed, Is.True);
    }
}
