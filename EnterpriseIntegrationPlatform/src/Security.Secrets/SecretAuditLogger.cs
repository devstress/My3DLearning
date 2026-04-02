using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Logs all secret access and rotation events as structured log entries for audit compliance.
/// </summary>
public sealed class SecretAuditLogger
{
    private readonly ILogger<SecretAuditLogger> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SecretAuditLogger"/>.
    /// </summary>
    /// <param name="logger">The logger instance for structured output.</param>
    public SecretAuditLogger(ILogger<SecretAuditLogger> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Records a secret audit event with structured data.
    /// </summary>
    /// <param name="auditEvent">The audit event to log.</param>
    public void Log(SecretAuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        _logger.Log(
            auditEvent.Success ? LogLevel.Information : LogLevel.Warning,
            "SecretAudit: Action={Action} Key={SecretKey} Version={Version} Principal={Principal} Success={Success} Detail={Detail} Timestamp={Timestamp}",
            auditEvent.Action,
            auditEvent.SecretKey,
            auditEvent.Version,
            auditEvent.Principal,
            auditEvent.Success,
            auditEvent.Detail,
            auditEvent.Timestamp);
    }

    /// <summary>
    /// Records a secret read event.
    /// </summary>
    /// <param name="key">The secret key that was read.</param>
    /// <param name="version">The version that was read, if known.</param>
    /// <param name="success">Whether the read operation succeeded.</param>
    /// <param name="principal">The identity performing the read.</param>
    public void LogRead(string key, string? version = null, bool success = true, string? principal = null)
    {
        Log(new SecretAuditEvent(SecretAccessAction.Read, key, DateTimeOffset.UtcNow, principal, version, success));
    }

    /// <summary>
    /// Records a secret write event.
    /// </summary>
    /// <param name="key">The secret key that was written.</param>
    /// <param name="version">The version that was created.</param>
    /// <param name="principal">The identity performing the write.</param>
    public void LogWrite(string key, string? version = null, string? principal = null)
    {
        Log(new SecretAuditEvent(SecretAccessAction.Write, key, DateTimeOffset.UtcNow, principal, version));
    }

    /// <summary>
    /// Records a secret delete event.
    /// </summary>
    /// <param name="key">The secret key that was deleted.</param>
    /// <param name="success">Whether the delete succeeded.</param>
    /// <param name="principal">The identity performing the delete.</param>
    public void LogDelete(string key, bool success = true, string? principal = null)
    {
        Log(new SecretAuditEvent(SecretAccessAction.Delete, key, DateTimeOffset.UtcNow, principal, Success: success));
    }

    /// <summary>
    /// Records a secret rotation event.
    /// </summary>
    /// <param name="key">The secret key that was rotated.</param>
    /// <param name="newVersion">The new version after rotation.</param>
    /// <param name="success">Whether the rotation succeeded.</param>
    /// <param name="detail">Additional detail about the rotation.</param>
    public void LogRotation(string key, string? newVersion = null, bool success = true, string? detail = null)
    {
        Log(new SecretAuditEvent(SecretAccessAction.Rotate, key, DateTimeOffset.UtcNow, Version: newVersion, Success: success, Detail: detail));
    }

    /// <summary>
    /// Records a cache hit event.
    /// </summary>
    /// <param name="key">The secret key served from cache.</param>
    public void LogCacheHit(string key)
    {
        Log(new SecretAuditEvent(SecretAccessAction.CacheHit, key, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Records a cache eviction event.
    /// </summary>
    /// <param name="key">The secret key evicted from cache.</param>
    /// <param name="detail">Reason for eviction.</param>
    public void LogCacheEvict(string key, string? detail = null)
    {
        Log(new SecretAuditEvent(SecretAccessAction.CacheEvict, key, DateTimeOffset.UtcNow, Detail: detail));
    }
}
