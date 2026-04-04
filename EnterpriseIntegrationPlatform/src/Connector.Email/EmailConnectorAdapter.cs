using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Connector.Email;

/// <summary>
/// Adapts <see cref="IEmailConnector"/> to the unified <see cref="IConnector"/> interface,
/// enabling email connectors to participate in the platform's connector registry.
/// </summary>
public sealed class EmailConnectorAdapter : IConnector
{
    private readonly IEmailConnector _emailConnector;
    private readonly ISmtpClientWrapper _smtpClient;
    private readonly ILogger<EmailConnectorAdapter> _logger;

    /// <summary>Initialises a new instance of <see cref="EmailConnectorAdapter"/>.</summary>
    /// <param name="name">The unique connector name (e.g. "email-notifications").</param>
    /// <param name="emailConnector">The underlying email connector.</param>
    /// <param name="smtpClient">The SMTP client wrapper for health probes.</param>
    /// <param name="logger">Logger instance.</param>
    public EmailConnectorAdapter(
        string name,
        IEmailConnector emailConnector,
        ISmtpClientWrapper smtpClient,
        ILogger<EmailConnectorAdapter> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(emailConnector);
        ArgumentNullException.ThrowIfNull(smtpClient);
        ArgumentNullException.ThrowIfNull(logger);

        Name = name;
        _emailConnector = emailConnector;
        _smtpClient = smtpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ConnectorType ConnectorType => ConnectorType.Email;

    /// <inheritdoc />
    public async Task<ConnectorResult> SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        ConnectorSendOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(options);

        var recipient = options.Destination
            ?? throw new InvalidOperationException(
                "ConnectorSendOptions.Destination must be set to a recipient email address.");

        options.Properties.TryGetValue("Subject", out var subject);

        try
        {
            await _emailConnector.SendAsync(
                envelope,
                recipient,
                subject,
                static payload => System.Text.Json.JsonSerializer.Serialize(payload),
                cancellationToken);

            _logger.LogInformation(
                "Email sent to '{Recipient}' for connector '{ConnectorName}'",
                recipient, Name);

            return ConnectorResult.Ok(Name, $"Email sent to {recipient}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Email send failed for connector '{ConnectorName}'", Name);

            return ConnectorResult.Fail(Name, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_smtpClient.IsConnected)
            {
                _logger.LogDebug(
                    "Health probe for email connector '{ConnectorName}': Healthy (already connected)",
                    Name);
                return true;
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Health probe for email connector '{ConnectorName}' failed", Name);
            return false;
        }
    }
}
