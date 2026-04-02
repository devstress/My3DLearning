using EnterpriseIntegrationPlatform.Security.Secrets;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.SecretsTests;

[TestFixture]
public sealed class SecretAuditLoggerTests
{
    private ILogger<SecretAuditLogger> _mockLogger = null!;
    private SecretAuditLogger _auditLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = Substitute.For<ILogger<SecretAuditLogger>>();
        _auditLogger = new SecretAuditLogger(_mockLogger);
    }

    [Test]
    public void LogRead_Success_LogsAtInformationLevel()
    {
        _auditLogger.LogRead("db-password", "1", success: true, principal: "admin");

        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void LogRead_Failure_LogsAtWarningLevel()
    {
        _auditLogger.LogRead("db-password", success: false);

        _mockLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void LogWrite_LogsAtInformationLevel()
    {
        _auditLogger.LogWrite("api-key", "2", "deployer");

        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void LogDelete_Success_LogsAtInformationLevel()
    {
        _auditLogger.LogDelete("old-secret", success: true);

        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void LogDelete_Failure_LogsAtWarningLevel()
    {
        _auditLogger.LogDelete("missing-key", success: false);

        _mockLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void LogRotation_Success_LogsAtInformationLevel()
    {
        _auditLogger.LogRotation("rotated-key", "3", success: true, detail: "Auto-rotation");

        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void LogRotation_Failure_LogsAtWarningLevel()
    {
        _auditLogger.LogRotation("failed-key", success: false, detail: "Vault unreachable");

        _mockLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void LogCacheHit_LogsAtInformationLevel()
    {
        _auditLogger.LogCacheHit("cached-key");

        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void LogCacheEvict_LogsAtInformationLevel()
    {
        _auditLogger.LogCacheEvict("evicted-key", "TTL expired");

        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void Log_NullEvent_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => _auditLogger.Log(null!));
    }

    [Test]
    public void Log_CustomEvent_LogsCorrectly()
    {
        var evt = new SecretAuditEvent(
            SecretAccessAction.List,
            "prefix/*",
            DateTimeOffset.UtcNow,
            Principal: "scanner",
            Success: true);

        _auditLogger.Log(evt);

        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
