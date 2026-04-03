namespace EnterpriseIntegrationPlatform.Connectors;

/// <summary>
/// Identifies the transport protocol of a connector.
/// </summary>
public enum ConnectorType
{
    /// <summary>HTTP/HTTPS REST API connector.</summary>
    Http,

    /// <summary>SFTP file transfer connector.</summary>
    Sftp,

    /// <summary>SMTP email connector.</summary>
    Email,

    /// <summary>Local or network file system connector.</summary>
    File,
}
