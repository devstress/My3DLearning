using EnterpriseIntegrationPlatform.Ingestion.Nats;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class NatsHealthCheckTests
{
    [Test]
    public void Constructor_AcceptsDependencies()
    {
        var connection = Substitute.For<INatsConnection>();
        var logger = Substitute.For<ILogger<NatsHealthCheck>>();

        var healthCheck = new NatsHealthCheck(connection, logger);

        Assert.That(healthCheck, Is.Not.Null);
        Assert.That(healthCheck, Is.InstanceOf<IHealthCheck>());
    }

    [Test]
    public async Task CheckHealthAsync_Unhealthy_WhenConnectionFails()
    {
        // NatsJSContext requires a concrete NatsConnection cast, so passing an
        // INatsConnection mock will cause an InvalidCastException — which the
        // health check catches and reports as Unhealthy.
        var connection = Substitute.For<INatsConnection>();
        var logger = Substitute.For<ILogger<NatsHealthCheck>>();
        var healthCheck = new NatsHealthCheck(connection, logger);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "nats", healthCheck, HealthStatus.Unhealthy, []),
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Description, Does.Contain("unreachable"));
            Assert.That(result.Exception, Is.Not.Null);
        });
    }

    [Test]
    public async Task CheckHealthAsync_Unhealthy_WhenCancelled()
    {
        var connection = Substitute.For<INatsConnection>();
        var logger = Substitute.For<ILogger<NatsHealthCheck>>();
        var healthCheck = new NatsHealthCheck(connection, logger);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Even with a cancelled token, the cast to NatsConnection fails first,
        // producing an unhealthy result.
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "nats", healthCheck, HealthStatus.Unhealthy, []),
            },
            cts.Token);

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
    }
}
