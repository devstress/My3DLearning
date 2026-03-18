using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Connector.Email;

/// <summary>
/// Sends <see cref="IntegrationEnvelope{T}"/> payloads as email messages via SMTP.
/// </summary>
public interface IEmailConnector
{
    /// <summary>
    /// Sends the envelope payload to a single recipient.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="toAddress">Recipient email address.</param>
    /// <param name="subject">
    /// Email subject. When <c>null</c>, the configured
    /// <see cref="EmailConnectorOptions.DefaultSubjectTemplate"/> is used.
    /// </param>
    /// <param name="bodyBuilder">Function that converts the payload to an email body string.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        string toAddress,
        string? subject,
        Func<T, string> bodyBuilder,
        CancellationToken ct);

    /// <summary>
    /// Sends the envelope payload to multiple recipients.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="toAddresses">List of recipient email addresses.</param>
    /// <param name="subject">
    /// Email subject. When <c>null</c>, the configured
    /// <see cref="EmailConnectorOptions.DefaultSubjectTemplate"/> is used.
    /// </param>
    /// <param name="bodyBuilder">Function that converts the payload to an email body string.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        IReadOnlyList<string> toAddresses,
        string? subject,
        Func<T, string> bodyBuilder,
        CancellationToken ct);
}
