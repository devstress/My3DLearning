using EnterpriseIntegrationPlatform.Admin.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class PlatformStatusServiceTests
{
    private readonly HealthCheckService _healthCheckService =
        Substitute.For<HealthCheckService>();
    private readonly ILogger<PlatformStatusService> _logger =
        Substitute.For<ILogger<PlatformStatusService>>();

    [Fact]
    public async Task GetStatusAsync_WhenAllHealthy_ReturnsHealthyOverall()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["self"] = new(HealthStatus.Healthy, "OK", TimeSpan.FromMilliseconds(1), null, null),
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(5));
        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(report);
        var service = new PlatformStatusService(_healthCheckService, _logger);

        var result = await service.GetStatusAsync();

        result.Overall.Should().Be("Healthy");
        result.Components.Should().HaveCount(1);
        result.Components[0].Name.Should().Be("self");
        result.Components[0].Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetStatusAsync_WhenComponentUnhealthy_ReturnsUnhealthyOverall()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["cassandra"] = new(HealthStatus.Unhealthy, "Connection refused", TimeSpan.FromMilliseconds(500), null, null),
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(500));
        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(report);
        var service = new PlatformStatusService(_healthCheckService, _logger);

        var result = await service.GetStatusAsync();

        result.Overall.Should().Be("Unhealthy");
        result.Components[0].Description.Should().Be("Connection refused");
    }

    [Fact]
    public async Task GetStatusAsync_WhenHealthCheckThrows_ReturnsUnhealthyGracefully()
    {
        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("health check failed"));
        var service = new PlatformStatusService(_healthCheckService, _logger);

        var result = await service.GetStatusAsync();

        result.Overall.Should().Be("Unhealthy");
        result.Components.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatusAsync_AlwaysPopulatesCheckedAt()
    {
        var entries = new Dictionary<string, HealthReportEntry>();
        var report = new HealthReport(entries, TimeSpan.Zero);
        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(report);
        var service = new PlatformStatusService(_healthCheckService, _logger);

        var before = DateTimeOffset.UtcNow;
        var result = await service.GetStatusAsync();
        var after = DateTimeOffset.UtcNow;

        result.CheckedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task GetStatusAsync_PopulatesComponentDuration()
    {
        var duration = TimeSpan.FromMilliseconds(42);
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["self"] = new(HealthStatus.Healthy, null, duration, null, null),
        };
        var report = new HealthReport(entries, duration);
        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(report);
        var service = new PlatformStatusService(_healthCheckService, _logger);

        var result = await service.GetStatusAsync();

        result.Components[0].Duration.Should().Be(duration);
    }
}
