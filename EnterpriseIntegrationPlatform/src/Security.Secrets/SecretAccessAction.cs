namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Describes the type of secret access operation for audit logging.
/// </summary>
public enum SecretAccessAction
{
    /// <summary>A secret was read.</summary>
    Read,

    /// <summary>A secret was created or updated.</summary>
    Write,

    /// <summary>A secret was deleted.</summary>
    Delete,

    /// <summary>Secret keys were listed.</summary>
    List,

    /// <summary>A secret was rotated.</summary>
    Rotate,

    /// <summary>A secret was served from cache.</summary>
    CacheHit,

    /// <summary>A secret cache entry expired or was evicted.</summary>
    CacheEvict
}
