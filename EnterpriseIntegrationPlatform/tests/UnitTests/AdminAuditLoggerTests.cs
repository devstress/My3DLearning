using System.Security.Claims;
using EnterpriseIntegrationPlatform.Admin.Api.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class AdminAuditLoggerTests
{
    private ILogger<AdminAuditLogger> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<AdminAuditLogger>>();
    }

    [Test]
    public void LogAction_WithValidPrincipal_DoesNotThrow()
    {
        var auditLogger = new AdminAuditLogger(_logger);
        var principal = BuildPrincipal("abcd1234****");

        var act = () => auditLogger.LogAction("QueryMessages", "corr-123", principal);

        Assert.DoesNotThrow(() => act());
    }

    [Test]
    public void LogAction_WithNullPrincipal_DoesNotThrow()
    {
        var auditLogger = new AdminAuditLogger(_logger);

        var act = () => auditLogger.LogAction("QueryMessages", null, null);

        Assert.DoesNotThrow(() => act());
    }

    [Test]
    public void LogAction_WithNullTargetId_DoesNotThrow()
    {
        var auditLogger = new AdminAuditLogger(_logger);
        var principal = BuildPrincipal("abcd****");

        var act = () => auditLogger.LogAction("GetPlatformStatus", null, principal);

        Assert.DoesNotThrow(() => act());
    }

    [Test]
    public void LogAction_WithNoPrincipalClaim_DoesNotThrow()
    {
        var auditLogger = new AdminAuditLogger(_logger);
        var emptyPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        var act = () => auditLogger.LogAction("UpdateStatus", "msg-456", emptyPrincipal);

        Assert.DoesNotThrow(() => act());
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
