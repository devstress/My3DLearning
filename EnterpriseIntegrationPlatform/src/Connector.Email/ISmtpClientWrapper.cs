using MimeKit;

namespace EnterpriseIntegrationPlatform.Connector.Email;

/// <summary>
/// Thin abstraction over a MailKit SMTP client that enables unit-testing without a live server.
/// </summary>
public interface ISmtpClientWrapper
{
    /// <summary>
    /// Connects to the SMTP server.
    /// </summary>
    /// <param name="host">Server hostname.</param>
    /// <param name="port">Server port.</param>
    /// <param name="useTls">When <c>true</c>, negotiates STARTTLS.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ConnectAsync(string host, int port, bool useTls, CancellationToken ct);

    /// <summary>Authenticates with the SMTP server.</summary>
    /// <param name="username">SMTP username.</param>
    /// <param name="password">SMTP password.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AuthenticateAsync(string username, string password, CancellationToken ct);

    /// <summary>Sends a MIME message.</summary>
    /// <param name="message">The message to send.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAsync(MimeMessage message, CancellationToken ct);

    /// <summary>Disconnects from the SMTP server.</summary>
    /// <param name="quit">When <c>true</c>, sends the SMTP QUIT command before disconnecting.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DisconnectAsync(bool quit, CancellationToken ct);

    /// <summary>Gets a value indicating whether the client is currently connected.</summary>
    bool IsConnected { get; }
}
