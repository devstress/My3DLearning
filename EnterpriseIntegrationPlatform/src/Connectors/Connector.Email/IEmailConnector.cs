namespace EnterpriseIntegrationPlatform.Connectors.Email;

/// <summary>
/// Sends and receives messages via email (SMTP/IMAP).
/// </summary>
public interface IEmailConnector
{
    /// <summary>Sends an email message.</summary>
    Task SendAsync(
        EmailMessage message,
        CancellationToken ct = default);

    /// <summary>Retrieves unread messages from the configured mailbox.</summary>
    Task<IReadOnlyList<EmailMessage>> ReceiveAsync(
        int maxMessages = 10,
        CancellationToken ct = default);
}

/// <summary>
/// Represents an email message for the email connector.
/// </summary>
/// <param name="From">Sender address.</param>
/// <param name="To">Recipient addresses.</param>
/// <param name="Subject">Email subject line.</param>
/// <param name="Body">Email body content.</param>
/// <param name="IsHtml">True if <paramref name="Body"/> is HTML.</param>
public record EmailMessage(
    string From,
    IReadOnlyList<string> To,
    string Subject,
    string Body,
    bool IsHtml = false);
