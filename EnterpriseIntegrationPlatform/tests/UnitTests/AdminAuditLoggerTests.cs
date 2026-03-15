using System.Security.Claims;
using EnterpriseIntegrationPlatform.Admin.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class AdminAuditLoggerTests
{
    private readonly ILogger<AdminAuditLogger> _logger = Substitute.For<ILogger<AdminAuditLogger>>();

    [Fact]
    public void LogAction_WithValidPrincipal_DoesNotThrow()
    {
        var auditLogger = new AdminAuditLogger(_logger);
        var principal = BuildPrincipal("abcd1234****");

        var act = () => auditLogger.LogAction("QueryMessages", "corr-123", principal);

        act.Should().NotThrow();
    }

    [Fact]
    public void LogAction_WithNullPrincipal_DoesNotThrow()
    {
        var auditLogger = new AdminAuditLogger(_logger);

        var act = () => auditLogger.LogAction("QueryMessages", null, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void LogAction_WithNullTargetId_DoesNotThrow()
    {
        var auditLogger = new AdminAuditLogger(_logger);
        var principal = BuildPrincipal("abcd****");

        var act = () => auditLogger.LogAction("GetPlatformStatus", null, principal);

        act.Should().NotThrow();
    }

    [Fact]
    public void LogAction_WithNoPrincipalClaim_DoesNotThrow()
    {
        var auditLogger = new AdminAuditLogger(_logger);
        var emptyPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        var act = () => auditLogger.LogAction("UpdateStatus", "msg-456", emptyPrincipal);

        act.Should().NotThrow();
    }

    private static ClaimsPrincipal BuildPrincipal(string keyPrefix)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim("apikey_prefix", keyPrefix),
        };
        var identity = new ClaimsIdentity(claims, "ApiKey");
        return new ClaimsPrincipal(identity);
    }
}
