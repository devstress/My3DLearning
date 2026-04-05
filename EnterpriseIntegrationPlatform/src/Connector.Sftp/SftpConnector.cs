using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Connector.Sftp;

/// <summary>
/// SFTP connector that uploads and downloads files on behalf of the integration platform,
/// writing a JSON metadata sidecar alongside every uploaded data file.
/// Connections are acquired from and returned to an <see cref="ISftpConnectionPool"/>.
/// </summary>
public sealed class SftpConnector : ISftpConnector
{
    private readonly ISftpConnectionPool _pool;
    private readonly SftpConnectorOptions _options;
    private readonly ILogger<SftpConnector> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="SftpConnector"/>.
    /// </summary>
    /// <param name="pool">Connection pool.</param>
    /// <param name="options">Connector options.</param>
    /// <param name="logger">Logger instance.</param>
    public SftpConnector(
        ISftpConnectionPool pool,
        IOptions<SftpConnectorOptions> options,
        ILogger<SftpConnector> logger)
    {
        _pool = pool;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync<T>(
        IntegrationEnvelope<T> envelope,
        string fileName,
        Func<T, byte[]> serializer,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);

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

        var client = await _pool.AcquireAsync(ct);
        try
        {
            using var dataStream = new MemoryStream(bytes);
            client.UploadFile(dataStream, remotePath);

            using var metaStream = new MemoryStream(meta);
            client.UploadFile(metaStream, metaPath);

            _logger.LogInformation(
                "Uploaded {RemotePath} for correlation {CorrelationId}",
                remotePath, envelope.CorrelationId);
        }
        finally
        {
            _pool.Release(client);
        }

        return remotePath;
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadAsync(string remotePath, CancellationToken ct)
    {
        var client = await _pool.AcquireAsync(ct);
        try
        {
            using var stream = client.DownloadFile(remotePath);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            _logger.LogInformation("Downloaded {RemotePath}", remotePath);
            return ms.ToArray();
        }
        finally
        {
            _pool.Release(client);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListFilesAsync(string remotePath, CancellationToken ct)
    {
        var client = await _pool.AcquireAsync(ct);
        try
        {
            var files = client.ListFiles(remotePath).ToList();
            _logger.LogInformation("Listed {Count} files at {RemotePath}", files.Count, remotePath);
            return files;
        }
        finally
        {
            _pool.Release(client);
        }
    }
}
