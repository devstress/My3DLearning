using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace EnterpriseIntegrationPlatform.Connector.Email;

/// <summary>
/// Email connector that builds <see cref="MimeMessage"/> instances from
/// <see cref="IntegrationEnvelope{T}"/> payloads and sends them via SMTP.
/// Platform correlation headers are added to every outgoing message.
/// </summary>
public sealed class EmailConnector : IEmailConnector
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string MessageIdHeader = "X-Message-Id";

    private readonly ISmtpClientWrapper _smtpClient;
    private readonly EmailConnectorOptions _options;
    private readonly ILogger<EmailConnector> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="EmailConnector"/>.
    /// </summary>
    /// <param name="smtpClient">Abstracted SMTP client wrapper.</param>
    /// <param name="options">Connector options.</param>
    /// <param name="logger">Logger instance.</param>
    public EmailConnector(
        ISmtpClientWrapper smtpClient,
        IOptions<EmailConnectorOptions> options,
        ILogger<EmailConnector> logger)
    {
        _smtpClient = smtpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        string toAddress,
        string? subject,
        Func<T, string> bodyBuilder,
        CancellationToken ct) =>
        SendAsync(envelope, (IReadOnlyList<string>)[toAddress], subject, bodyBuilder, ct);

    /// <inheritdoc />
    public async Task SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        IReadOnlyList<string> toAddresses,
        string? subject,
        Func<T, string> bodyBuilder,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(bodyBuilder);

        if (toAddresses is null || toAddresses.Count == 0)
            throw new ArgumentException("At least one recipient address is required.", nameof(toAddresses));

        var resolvedSubject = subject
            ?? _options.DefaultSubjectTemplate.Replace("{MessageType}", envelope.MessageType);

        var body = bodyBuilder(envelope.Payload);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(string.Empty, _options.DefaultFrom));

        foreach (var addr in toAddresses)
            message.To.Add(new MailboxAddress(string.Empty, addr));

        message.Subject = resolvedSubject;
        message.Body = new TextPart(TextFormat.Plain) { Text = body };

        message.Headers.Add(CorrelationIdHeader, envelope.CorrelationId.ToString());
        message.Headers.Add(MessageIdHeader, envelope.MessageId.ToString());

        _logger.LogInformation(
            "Sending email for correlation {CorrelationId} to {RecipientCount} recipient(s)",
            envelope.CorrelationId, toAddresses.Count);

        await _smtpClient.ConnectAsync(_options.SmtpHost, _options.SmtpPort, _options.UseTls, ct);
        try
        {
            await _smtpClient.AuthenticateAsync(_options.Username, _options.Password, ct);
            await _smtpClient.SendAsync(message, ct);
        }
        finally
        {
            await _smtpClient.DisconnectAsync(quit: true, ct);
        }
    }
}
