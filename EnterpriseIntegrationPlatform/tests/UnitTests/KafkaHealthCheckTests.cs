using Confluent.Kafka;
using EnterpriseIntegrationPlatform.Ingestion.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class KafkaHealthCheckTests
{
    // ------------------------------------------------------------------ //
    // Constructor validation
    // ------------------------------------------------------------------ //

    [Test]
    public void Constructor_NullProducer_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new KafkaHealthCheck(null!, NullLogger<KafkaHealthCheck>.Instance),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var producer = Substitute.For<IProducer<string, byte[]>>();
        Assert.That(
            () => new KafkaHealthCheck(producer, null!),
            Throws.ArgumentNullException);
    }

    // ------------------------------------------------------------------ //
    // Healthy path
    // ------------------------------------------------------------------ //

    [Test]
    public async Task CheckHealthAsync_ProducerHasName_ReturnsHealthy()
    {
        var producer = Substitute.For<IProducer<string, byte[]>>();
        producer.Name.Returns("rdkafka#producer-1");
        var sut = new KafkaHealthCheck(producer, NullLogger<KafkaHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        Assert.That(result.Description, Does.Contain("rdkafka#producer-1"));
    }

    // ------------------------------------------------------------------ //
    // Unhealthy paths
    // ------------------------------------------------------------------ //

    [Test]
    public async Task CheckHealthAsync_ProducerNameIsEmpty_ReturnsUnhealthy()
    {
        var producer = Substitute.For<IProducer<string, byte[]>>();
        producer.Name.Returns(string.Empty);
        var sut = new KafkaHealthCheck(producer, NullLogger<KafkaHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Description, Does.Contain("no name"));
    }

    [Test]
    public async Task CheckHealthAsync_ProducerThrows_ReturnsUnhealthy()
    {
        var producer = Substitute.For<IProducer<string, byte[]>>();
        producer.Name.Throws(new KafkaException(new Error(ErrorCode.BrokerNotAvailable)));
        var sut = new KafkaHealthCheck(producer, NullLogger<KafkaHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Exception, Is.Not.Null);
    }
}
