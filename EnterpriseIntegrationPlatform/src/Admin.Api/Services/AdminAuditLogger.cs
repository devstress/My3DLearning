using System.Security.Claims;

namespace EnterpriseIntegrationPlatform.Admin.Api.Services;

/// <summary>
/// Records immutable audit events for all Admin API operations.
/// Events are emitted as structured log entries that flow through OpenTelemetry
/// to Grafana Loki, providing a durable, queryable, write-once audit trail.
/// </summary>
public sealed class AdminAuditLogger(ILogger<AdminAuditLogger> logger)
{
    /// <summary>
    /// Logs an admin action to the structured audit trail.
    /// </summary>
    /// <param name="action">Name of the action performed (e.g. "QueryMessages", "UpdateStatus").</param>
    /// <param name="targetId">Optional identifier of the resource acted upon.</param>
    /// <param name="principal">The authenticated principal performing the action.</param>
    public void LogAction(string action, string? targetId, ClaimsPrincipal? principal)
    {
        var maskedKey = MaskApiKey(principal?.FindFirst("apikey_prefix")?.Value);
        logger.LogInformation(
            "AdminAudit Action={Action} TargetId={TargetId} ApiKey={ApiKey} At={At}",
            action,
            targetId ?? "N/A",
            maskedKey,
            DateTimeOffset.UtcNow);
    }

    private static string MaskApiKey(string? keyPrefix) =>
        string.IsNullOrEmpty(keyPrefix) ? "****" : keyPrefix;
}
