namespace EnterpriseIntegrationPlatform.Connector.Sftp;

/// <summary>
/// Configuration options for the SFTP connector.
/// </summary>
public sealed class SftpConnectorOptions
{
    /// <summary>SFTP server hostname or IP address (required).</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SFTP server port. Default is 22.</summary>
    public int Port { get; set; } = 22;

    /// <summary>SFTP username (required).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SFTP password (required). In production, supply this via a secrets manager
    /// rather than plain configuration.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Root path on the SFTP server. Default is <c>/</c>.</summary>
    public string RootPath { get; set; } = "/";

    /// <summary>Connection timeout in milliseconds. Default is 10000.</summary>
    public int TimeoutMs { get; set; } = 10000;
}
