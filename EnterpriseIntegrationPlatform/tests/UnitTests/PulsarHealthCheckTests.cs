using DotPulsar.Abstractions;
using EnterpriseIntegrationPlatform.Ingestion.Pulsar;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PulsarHealthCheckTests
{
    // ------------------------------------------------------------------ //
    // Constructor validation
    // ------------------------------------------------------------------ //

    [Test]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new PulsarHealthCheck(null!, NullLogger<PulsarHealthCheck>.Instance),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var client = Substitute.For<IPulsarClient>();
        Assert.That(
            () => new PulsarHealthCheck(client, null!),
            Throws.ArgumentNullException);
    }

    // ------------------------------------------------------------------ //
    // Health check behavior
    // ------------------------------------------------------------------ //

    [Test]
    public void Constructor_ValidArgs_DoesNotThrow()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarHealthCheck(client, NullLogger<PulsarHealthCheck>.Instance);
        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public async Task CheckHealthAsync_ClientIsAvailable_ReturnsHealthy()
    {
        var client = Substitute.For<IPulsarClient>();
        var sut = new PulsarHealthCheck(client, NullLogger<PulsarHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // NewProducer() is a static extension method that creates a builder
        // without requiring an actual Pulsar connection, so it succeeds.
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        Assert.That(result.Description, Does.Contain("operational"));
    }
}
