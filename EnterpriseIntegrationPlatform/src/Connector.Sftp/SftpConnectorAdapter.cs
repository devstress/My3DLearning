using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Connector.Sftp;

/// <summary>
/// Adapts <see cref="ISftpConnector"/> to the unified <see cref="IConnector"/> interface,
/// enabling SFTP connectors to participate in the platform's connector registry.
/// </summary>
public sealed class SftpConnectorAdapter : IConnector
{
    private readonly ISftpConnector _sftpConnector;
    private readonly ISftpClient _sftpClient;
    private readonly ILogger<SftpConnectorAdapter> _logger;

    /// <summary>Initialises a new instance of <see cref="SftpConnectorAdapter"/>.</summary>
    /// <param name="name">The unique connector name (e.g. "sftp-vendor-a").</param>
    /// <param name="sftpConnector">The underlying SFTP connector.</param>
    /// <param name="sftpClient">The SFTP client for health probes.</param>
    /// <param name="logger">Logger instance.</param>
    public SftpConnectorAdapter(
        string name,
        ISftpConnector sftpConnector,
        ISftpClient sftpClient,
        ILogger<SftpConnectorAdapter> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(sftpConnector);
        ArgumentNullException.ThrowIfNull(sftpClient);
        ArgumentNullException.ThrowIfNull(logger);

        Name = name;
        _sftpConnector = sftpConnector;
        _sftpClient = sftpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ConnectorType ConnectorType => ConnectorType.Sftp;

    /// <inheritdoc />
    public async Task<ConnectorResult> SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        ConnectorSendOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(options);

        var fileName = options.Destination
            ?? $"{envelope.MessageId}-{envelope.MessageType}.json";

        try
        {
            var remotePath = await _sftpConnector.UploadAsync(
                envelope,
                fileName,
                static payload => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload),
                cancellationToken);

            _logger.LogInformation(
                "SFTP upload to '{RemotePath}' succeeded for connector '{ConnectorName}'",
                remotePath, Name);

            return ConnectorResult.Ok(Name, $"Uploaded to {remotePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SFTP upload failed for connector '{ConnectorName}'", Name);

            return ConnectorResult.Fail(Name, ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _sftpClient.Connect();
            try
            {
                var connected = _sftpClient.IsConnected;

                _logger.LogDebug(
                    "Health probe for SFTP connector '{ConnectorName}': {Status}",
                    Name, connected ? "Healthy" : "Unhealthy");

                return Task.FromResult(connected);
            }
            finally
            {
                _sftpClient.Disconnect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Health probe for SFTP connector '{ConnectorName}' failed", Name);
            return Task.FromResult(false);
        }
    }
}
