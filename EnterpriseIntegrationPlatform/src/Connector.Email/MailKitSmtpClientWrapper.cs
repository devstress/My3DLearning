using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EnterpriseIntegrationPlatform.Connector.Email;

/// <summary>
/// Production <see cref="ISmtpClientWrapper"/> implementation backed by MailKit's
/// <see cref="SmtpClient"/>.
/// </summary>
public sealed class MailKitSmtpClientWrapper : ISmtpClientWrapper, IDisposable
{
    private readonly SmtpClient _client = new();

    /// <inheritdoc />
    public bool IsConnected => _client.IsConnected;

    /// <inheritdoc />
    public Task ConnectAsync(string host, int port, bool useTls, CancellationToken ct) =>
        _client.ConnectAsync(host, port, useTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);

    /// <inheritdoc />
    public Task AuthenticateAsync(string username, string password, CancellationToken ct) =>
        _client.AuthenticateAsync(username, password, ct);

    /// <inheritdoc />
    public Task SendAsync(MimeMessage message, CancellationToken ct) =>
        _client.SendAsync(message, ct);

    /// <inheritdoc />
    public Task DisconnectAsync(bool quit, CancellationToken ct) =>
        _client.DisconnectAsync(quit, ct);

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();
}
