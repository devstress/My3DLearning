using Cassandra;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class CassandraHealthCheckTests
{
    private readonly ICassandraSessionFactory _sessionFactory = Substitute.For<ICassandraSessionFactory>();
    private readonly ILogger<CassandraHealthCheck> _logger = Substitute.For<ILogger<CassandraHealthCheck>>();
    private readonly CassandraHealthCheck _healthCheck;

    public CassandraHealthCheckTests()
    {
        _healthCheck = new CassandraHealthCheck(_sessionFactory, _logger);
    }

    [Fact]
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
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("5.0");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenCassandraIsNotReachable()
    {
        // Arrange
        _sessionFactory.GetSessionAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new NoHostAvailableException(new Dictionary<System.Net.IPEndPoint, Exception>()));

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenSessionThrows()
    {
        // Arrange
        _sessionFactory.GetSessionAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection refused"));

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }
}
