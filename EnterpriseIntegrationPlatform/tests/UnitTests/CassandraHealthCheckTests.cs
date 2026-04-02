using Cassandra;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class CassandraHealthCheckTests
{
    private ICassandraSessionFactory _sessionFactory = null!;
    private ILogger<CassandraHealthCheck> _logger = null!;
    private CassandraHealthCheck _healthCheck = null!;

    [SetUp]
    public void SetUp()
    {
        _sessionFactory = Substitute.For<ICassandraSessionFactory>();
        _logger = Substitute.For<ILogger<CassandraHealthCheck>>();
        _healthCheck = new CassandraHealthCheck(_sessionFactory, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        (_sessionFactory as IDisposable)?.Dispose();
    }

    [Test]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenCassandraIsReachable()
    {
        // Arrange
        var session = Substitute.For<ISession>();
        var row = Substitute.For<Row>();
        row.GetValue<string>("release_version").Returns("5.0");

        var rowSet = Substitute.For<RowSet>();
        rowSet.GetEnumerator().Returns(new List<Row> { row }.GetEnumerator());

        _sessionFactory.GetSessionAsync(Arg.Any<CancellationToken>()).Returns(session);
        session.ExecuteAsync(Arg.Any<IStatement>()).Returns(rowSet);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        Assert.That(result.Description, Does.Contain("5.0"));
    }

    [Test]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenCassandraIsNotReachable()
    {
        // Arrange
        _sessionFactory.GetSessionAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new NoHostAvailableException(new Dictionary<System.Net.IPEndPoint, Exception>()));

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Description, Does.Contain("not reachable"));
    }

    [Test]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenSessionThrows()
    {
        // Arrange
        _sessionFactory.GetSessionAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection refused"));

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Exception, Is.InstanceOf<InvalidOperationException>());
    }
}
