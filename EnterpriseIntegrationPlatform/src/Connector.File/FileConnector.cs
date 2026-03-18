using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Connector.FileSystem;

/// <summary>
/// File system connector that writes and reads <see cref="IntegrationEnvelope{T}"/> payloads
/// as files, with a JSON metadata sidecar for each written file.
/// </summary>
public sealed class FileConnector : IFileConnector
{
    private readonly IFileSystem _fileSystem;
    private readonly FileConnectorOptions _options;
    private readonly ILogger<FileConnector> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="FileConnector"/>.
    /// </summary>
    /// <param name="fileSystem">Abstracted file system.</param>
    /// <param name="options">Connector options.</param>
    /// <param name="logger">Logger instance.</param>
    public FileConnector(
        IFileSystem fileSystem,
        IOptions<FileConnectorOptions> options,
        ILogger<FileConnector> logger)
    {
        _fileSystem = fileSystem;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> WriteAsync<T>(
        IntegrationEnvelope<T> envelope,
        Func<T, byte[]> serializer,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (string.IsNullOrWhiteSpace(_options.RootDirectory))
            throw new ArgumentException("FileConnectorOptions.RootDirectory must not be empty.");

        var filename = ExpandPattern(_options.FilenamePattern, envelope);
        var filePath = Path.Combine(_options.RootDirectory, filename);
        var metaPath = filePath + ".meta.json";

        if (_options.CreateDirectoryIfNotExists)
            _fileSystem.CreateDirectory(_options.RootDirectory);

        if (!_options.OverwriteExisting && _fileSystem.FileExists(filePath))
            throw new InvalidOperationException(
                $"File '{filePath}' already exists and OverwriteExisting is false.");

        var bytes = serializer(envelope.Payload);
        await _fileSystem.WriteAllBytesAsync(filePath, bytes, ct);

        var meta = JsonSerializer.SerializeToUtf8Bytes(new
        {
            CorrelationId = envelope.CorrelationId,
            MessageId = envelope.MessageId,
            MessageType = envelope.MessageType,
            Timestamp = envelope.Timestamp
        });
        await _fileSystem.WriteAllBytesAsync(metaPath, meta, ct);

        _logger.LogInformation("Wrote {FilePath} for correlation {CorrelationId}", filePath, envelope.CorrelationId);
        return filePath;
    }

    /// <inheritdoc />
    public Task<byte[]> ReadAsync(string filePath, CancellationToken ct) =>
        _fileSystem.ReadAllBytesAsync(filePath, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListFilesAsync(
        string? subdirectory,
        string searchPattern,
        CancellationToken ct)
    {
        var directory = string.IsNullOrWhiteSpace(subdirectory)
            ? _options.RootDirectory
            : Path.Combine(_options.RootDirectory, subdirectory);

        var files = _fileSystem.GetFiles(directory, searchPattern).ToList();
        _logger.LogInformation("Listed {Count} files in {Directory}", files.Count, directory);
        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    private static string ExpandPattern<T>(string pattern, IntegrationEnvelope<T> envelope) =>
        pattern
            .Replace("{MessageId}", envelope.MessageId.ToString())
            .Replace("{MessageType}", envelope.MessageType)
            .Replace("{CorrelationId}", envelope.CorrelationId.ToString())
            .Replace("{Timestamp:yyyyMMddHHmmss}", envelope.Timestamp.ToString("yyyyMMddHHmmss"));
}
