using System.Security.Claims;
using EnterpriseIntegrationPlatform.Admin.Api;
using EnterpriseIntegrationPlatform.Admin.Api.Services;
using EnterpriseIntegrationPlatform.Processing.Replay;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class AdminApiServiceTests
{
    private HealthCheckService _healthCheckService = null!;
    private ILogger<PlatformStatusService> _statusLogger = null!;
    private PlatformStatusService _statusService = null!;

    private IMessageReplayer _replayer = null!;
    private ILogger<DlqManagementService> _dlqLogger = null!;
    private DlqManagementService _dlqService = null!;

    private ILogger<AdminAuditLogger> _auditLoggerInner = null!;
    private AdminAuditLogger _auditLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _healthCheckService = Substitute.For<HealthCheckService>();
        _statusLogger = Substitute.For<ILogger<PlatformStatusService>>();
        _statusService = new PlatformStatusService(_healthCheckService, _statusLogger);

        _replayer = Substitute.For<IMessageReplayer>();
        _dlqLogger = Substitute.For<ILogger<DlqManagementService>>();
        _dlqService = new DlqManagementService(_replayer, _dlqLogger);

        _auditLoggerInner = Substitute.For<ILogger<AdminAuditLogger>>();
        _auditLogger = new AdminAuditLogger(_auditLoggerInner);
    }

    // ── AdminApiOptions ──────────────────────────────────────────────

    [Test]
    public void SectionName_Always_ReturnsAdminApi()
    {
        Assert.That(AdminApiOptions.SectionName, Is.EqualTo("AdminApi"));
    }

    [Test]
    public void ApiKeys_Default_IsEmpty()
    {
        var options = new AdminApiOptions();

        Assert.That(options.ApiKeys, Is.Empty);
    }

    [Test]
    public void RateLimitPerMinute_Default_IsSixty()
    {
        var options = new AdminApiOptions();

        Assert.That(options.RateLimitPerMinute, Is.EqualTo(60));
    }

    [Test]
    public void Properties_WhenSet_RetainValues()
    {
        var keys = new[] { "key-1", "key-2" };

        var options = new AdminApiOptions
        {
            ApiKeys = keys,
            RateLimitPerMinute = 200,
        };

        Assert.That(options.ApiKeys, Is.EquivalentTo(keys));
        Assert.That(options.RateLimitPerMinute, Is.EqualTo(200));
    }

    // ── PlatformStatusService ────────────────────────────────────────

    [Test]
    public async Task GetStatusAsync_HealthyReport_ReturnsHealthyOverall()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["cassandra"] = new(HealthStatus.Healthy, "OK", TimeSpan.FromMilliseconds(50), null, null),
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(50));

        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(report);

        var result = await _statusService.GetStatusAsync();

        Assert.That(result.Overall, Is.EqualTo(nameof(HealthStatus.Healthy)));
        Assert.That(result.Components, Has.Count.EqualTo(1));
        Assert.That(result.Components[0].Name, Is.EqualTo("cassandra"));
    }

    [Test]
    public async Task GetStatusAsync_UnhealthyReport_ReturnsUnhealthyWithComponents()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["cassandra"] = new(HealthStatus.Unhealthy, "Timeout", TimeSpan.FromSeconds(5), null, null),
            ["redis"] = new(HealthStatus.Healthy, "OK", TimeSpan.FromMilliseconds(10), null, null),
        };
        var report = new HealthReport(entries, TimeSpan.FromSeconds(5));

        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(report);

        var result = await _statusService.GetStatusAsync();

        Assert.That(result.Overall, Is.EqualTo(nameof(HealthStatus.Unhealthy)));
        Assert.That(result.Components, Has.Count.EqualTo(2));

        var cassandra = result.Components.Single(c => c.Name == "cassandra");
        Assert.That(cassandra.Status, Is.EqualTo(nameof(HealthStatus.Unhealthy)));
        Assert.That(cassandra.Description, Is.EqualTo("Timeout"));
    }

    [Test]
    public async Task GetStatusAsync_HealthCheckThrows_ReturnsUnhealthyWithEmptyComponents()
    {
        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _statusService.GetStatusAsync();

        Assert.That(result.Overall, Is.EqualTo(nameof(HealthStatus.Unhealthy)));
        Assert.That(result.Components, Is.Empty);
        Assert.That(result.TotalDuration, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public async Task GetStatusAsync_HealthyReport_CheckedAtIsPopulated()
    {
        var before = DateTimeOffset.UtcNow;
        var entries = new Dictionary<string, HealthReportEntry>();
        var report = new HealthReport(entries, TimeSpan.Zero);

        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(report);

        var result = await _statusService.GetStatusAsync();
        var after = DateTimeOffset.UtcNow;

        Assert.That(result.CheckedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(result.CheckedAt, Is.LessThanOrEqualTo(after));
    }

    // ── DlqManagementService ─────────────────────────────────────────

    [Test]
    public async Task ResubmitAsync_ValidFilter_DelegatesToReplayerAndReturnsResult()
    {
        var filter = new ReplayFilter { MessageType = "OrderCreated" };
        var expected = new ReplayResult
        {
            ReplayedCount = 5,
            FailedCount = 1,
            SkippedCount = 0,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAt = DateTimeOffset.UtcNow,
        };

        _replayer.ReplayAsync(filter, Arg.Any<CancellationToken>()).Returns(expected);

        var actual = await _dlqService.ResubmitAsync(filter);

        Assert.That(actual, Is.SameAs(expected));
        await _replayer.Received(1).ReplayAsync(filter, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResubmitAsync_FilterWithAllFields_PassesFilterThroughToReplayer()
    {
        var correlationId = Guid.NewGuid();
        var from = DateTimeOffset.UtcNow.AddHours(-2);
        var to = DateTimeOffset.UtcNow;
        var filter = new ReplayFilter
        {
            CorrelationId = correlationId,
            MessageType = "InvoicePaid",
            FromTimestamp = from,
            ToTimestamp = to,
        };

        ReplayFilter? captured = null;
        _replayer.ReplayAsync(Arg.Do<ReplayFilter>(f => captured = f), Arg.Any<CancellationToken>())
            .Returns(new ReplayResult
            {
                ReplayedCount = 0,
                FailedCount = 0,
                SkippedCount = 0,
                StartedAt = DateTimeOffset.UtcNow,
                CompletedAt = DateTimeOffset.UtcNow,
            });

        await _dlqService.ResubmitAsync(filter);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(captured.MessageType, Is.EqualTo("InvoicePaid"));
        Assert.That(captured.FromTimestamp, Is.EqualTo(from));
        Assert.That(captured.ToTimestamp, Is.EqualTo(to));
    }

    // ── AdminAuditLogger ─────────────────────────────────────────────

    [Test]
    public void LogAction_NullPrincipal_UsesMaskedKey()
    {
        // The MaskApiKey method returns "****" when keyPrefix is null/empty.
        // We verify by ensuring no exception and the logger is invoked.
        Assert.DoesNotThrow(() => _auditLogger.LogAction("GetStatus", "target-1", null));

        _auditLoggerInner.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("****")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void LogAction_PrincipalWithApiKeyPrefix_UsesPrefix()
    {
        var claims = new[] { new Claim("apikey_prefix", "abcd1234") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));

        Assert.DoesNotThrow(() => _auditLogger.LogAction("Resubmit", "dlq-42", principal));

        _auditLoggerInner.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("abcd1234")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
