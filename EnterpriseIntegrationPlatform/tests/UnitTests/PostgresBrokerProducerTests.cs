using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Postgres;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PostgresBrokerProducerTests
{
    private static readonly NullLogger<PostgresBrokerProducer> Logger = NullLogger<PostgresBrokerProducer>.Instance;

    private static IntegrationEnvelope<string> CreateEnvelope() => new()
    {
        MessageId = Guid.NewGuid(),
        CorrelationId = Guid.NewGuid(),
        Timestamp = DateTimeOffset.UtcNow,
        Source = "unit-test",
        MessageType = "test",
        Payload = "hello",
    };

    [Test]
    public void Constructor_NullFactory_Throws()
    {
        Assert.That(
            () => new PostgresBrokerProducer(null!, Logger),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("factory"));
    }

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        var factory = new PostgresConnectionFactory("Host=localhost;Database=eip;Username=t;Password=t");
        try
        {
            Assert.That(
                () => new PostgresBrokerProducer(factory, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("logger"));
        }
        finally { factory.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }

    [Test]
    public void PublishAsync_NullEnvelope_Throws()
    {
        var factory = new PostgresConnectionFactory("Host=localhost;Database=eip;Username=t;Password=t");
        try
        {
            var producer = new PostgresBrokerProducer(factory, Logger);

            Assert.That(
                async () => await producer.PublishAsync<string>(null!, "topic"),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("envelope"));
        }
        finally { factory.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }

    [Test]
    public void PublishAsync_NullTopic_ThrowsArgumentException()
    {
        var factory = new PostgresConnectionFactory("Host=localhost;Database=eip;Username=t;Password=t");
        try
        {
            var producer = new PostgresBrokerProducer(factory, Logger);
            var envelope = CreateEnvelope();

            Assert.That(
                async () => await producer.PublishAsync(envelope, null!),
                Throws.InstanceOf<ArgumentException>());
        }
        finally { factory.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }

    [Test]
    public void PublishAsync_EmptyTopic_ThrowsArgumentException()
    {
        var factory = new PostgresConnectionFactory("Host=localhost;Database=eip;Username=t;Password=t");
        try
        {
            var producer = new PostgresBrokerProducer(factory, Logger);
            var envelope = CreateEnvelope();

            Assert.That(
                async () => await producer.PublishAsync(envelope, ""),
                Throws.InstanceOf<ArgumentException>());
        }
        finally { factory.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }
}
