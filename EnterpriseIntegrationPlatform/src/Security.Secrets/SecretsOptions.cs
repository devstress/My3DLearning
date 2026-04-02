namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Configuration options for the secrets management subsystem.
/// Bind from the <c>Secrets</c> configuration section.
/// </summary>
public sealed class SecretsOptions
{
    /// <summary>
    /// The configuration section name used for binding.
    /// </summary>
    public const string SectionName = "Secrets";

    /// <summary>
    /// The secrets provider type to use. Supported values: "InMemory", "Vault", "AzureKeyVault".
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>
    /// Base URI of the HashiCorp Vault server (e.g. "https://vault.example.com:8200").
    /// </summary>
    public string? VaultAddress { get; set; }

    /// <summary>
    /// Authentication token for HashiCorp Vault.
    /// </summary>
    public string? VaultToken { get; set; }

    /// <summary>
    /// The Vault KV v2 mount path. Defaults to "secret".
    /// </summary>
    public string VaultMountPath { get; set; } = "secret";

    /// <summary>
    /// Base URI of the Azure Key Vault (e.g. "https://myvault.vault.azure.net").
    /// </summary>
    public string? AzureKeyVaultUri { get; set; }

    /// <summary>
    /// Azure AD tenant ID for authentication.
    /// </summary>
    public string? AzureTenantId { get; set; }

    /// <summary>
    /// Azure AD client ID for authentication.
    /// </summary>
    public string? AzureClientId { get; set; }

    /// <summary>
    /// Azure AD client secret for authentication.
    /// </summary>
    public string? AzureClientSecret { get; set; }

    /// <summary>
    /// Default cache TTL for the cached secret provider decorator.
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Interval at which the rotation background service checks for secrets needing rotation.
    /// </summary>
    public TimeSpan RotationCheckInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Whether to enable audit logging for all secret access events.
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;
}
