using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Connector.Sftp;

/// <summary>
/// SFTP connector that uploads and downloads files on behalf of the integration platform,
/// writing a JSON metadata sidecar alongside every uploaded data file.
/// </summary>
public sealed class SftpConnector : ISftpConnector
{
    private readonly ISftpClient _sftpClient;
    private readonly SftpConnectorOptions _options;
    private readonly ILogger<SftpConnector> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="SftpConnector"/>.
    /// </summary>
    /// <param name="sftpClient">Abstracted SFTP client.</param>
    /// <param name="options">Connector options.</param>
    /// <param name="logger">Logger instance.</param>
    public SftpConnector(
        ISftpClient sftpClient,
        IOptions<SftpConnectorOptions> options,
        ILogger<SftpConnector> logger)
    {
        _sftpClient = sftpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> UploadAsync<T>(
        IntegrationEnvelope<T> envelope,
        string fileName,
        Func<T, byte[]> serializer,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return Task.Run(() =>
        {
            var root = _options.RootPath.TrimEnd('/');
            var remotePath = $"{root}/{fileName}";
            var metaPath = remotePath + ".meta";

            var bytes = serializer(envelope.Payload);
            var meta = JsonSerializer.SerializeToUtf8Bytes(new
            {
                CorrelationId = envelope.CorrelationId,
                MessageId = envelope.MessageId,
                MessageType = envelope.MessageType,
                Timestamp = envelope.Timestamp
            });

            _sftpClient.Connect();
            try
            {
                using var dataStream = new MemoryStream(bytes);
                _sftpClient.UploadFile(dataStream, remotePath);

                using var metaStream = new MemoryStream(meta);
                _sftpClient.UploadFile(metaStream, metaPath);

                _logger.LogInformation(
                    "Uploaded {RemotePath} for correlation {CorrelationId}",
                    remotePath, envelope.CorrelationId);
            }
            finally
            {
                _sftpClient.Disconnect();
            }

            return remotePath;
        }, ct);
    }

    /// <inheritdoc />
    public Task<byte[]> DownloadAsync(string remotePath, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            _sftpClient.Connect();
            try
            {
                using var stream = _sftpClient.DownloadFile(remotePath);
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                _logger.LogInformation("Downloaded {RemotePath}", remotePath);
                return ms.ToArray();
            }
            finally
            {
                _sftpClient.Disconnect();
            }
        }, ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListFilesAsync(string remotePath, CancellationToken ct)
    {
        return Task.Run<IReadOnlyList<string>>(() =>
        {
            _sftpClient.Connect();
            try
            {
                var files = _sftpClient.ListFiles(remotePath).ToList();
                _logger.LogInformation("Listed {Count} files at {RemotePath}", files.Count, remotePath);
                return files;
            }
            finally
            {
                _sftpClient.Disconnect();
            }
        }, ct);
    }
}
