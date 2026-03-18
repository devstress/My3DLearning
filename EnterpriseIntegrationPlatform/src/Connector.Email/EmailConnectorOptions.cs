namespace EnterpriseIntegrationPlatform.Connector.Email;

/// <summary>
/// Configuration options for the Email connector.
/// </summary>
public sealed class EmailConnectorOptions
{
    /// <summary>SMTP server hostname or IP address (required).</summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>SMTP server port. Default is 587.</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>Whether to use STARTTLS when connecting. Default is <c>true</c>.</summary>
    public bool UseTls { get; set; } = true;

    /// <summary>SMTP authentication username (required).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP authentication password (required).</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Default sender address used when constructing outgoing messages (required).</summary>
    public string DefaultFrom { get; set; } = string.Empty;

    /// <summary>
    /// Default subject template. Use <c>{MessageType}</c> as a placeholder for the
    /// envelope's message type. Default is <c>{MessageType} notification</c>.
    /// </summary>
    public string DefaultSubjectTemplate { get; set; } = "{MessageType} notification";
}
