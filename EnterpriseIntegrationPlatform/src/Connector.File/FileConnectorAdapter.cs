using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Connector.FileSystem;

/// <summary>
/// Adapts <see cref="IFileConnector"/> to the unified <see cref="IConnector"/> interface,
/// enabling file connectors to participate in the platform's connector registry.
/// </summary>
public sealed class FileConnectorAdapter : IConnector
{
    private readonly IFileConnector _fileConnector;
    private readonly FileConnectorOptions _options;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FileConnectorAdapter> _logger;

    /// <summary>Initialises a new instance of <see cref="FileConnectorAdapter"/>.</summary>
    /// <param name="name">The unique connector name (e.g. "file-outbound").</param>
    /// <param name="fileConnector">The underlying file connector.</param>
    /// <param name="options">File connector options (used for health checks).</param>
    /// <param name="fileSystem">File system abstraction for health checks.</param>
    /// <param name="logger">Logger instance.</param>
    public FileConnectorAdapter(
        string name,
        IFileConnector fileConnector,
        IOptions<FileConnectorOptions> options,
        IFileSystem fileSystem,
        ILogger<FileConnectorAdapter> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fileConnector);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(logger);

        Name = name;
        _fileConnector = fileConnector;
        _options = options.Value;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ConnectorType ConnectorType => ConnectorType.File;

    /// <inheritdoc />
    public async Task<ConnectorResult> SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        ConnectorSendOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            var filePath = await _fileConnector.WriteAsync(
                envelope,
                static payload => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload),
                cancellationToken);

            _logger.LogInformation(
                "File written to '{FilePath}' for connector '{ConnectorName}'",
                filePath, Name);

            return ConnectorResult.Ok(Name, $"Written to {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "File write failed for connector '{ConnectorName}'", Name);

            return ConnectorResult.Fail(Name, ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.RootDirectory))
            {
                _logger.LogWarning(
                    "Health probe for file connector '{ConnectorName}': RootDirectory not configured",
                    Name);
                return Task.FromResult(false);
            }

            _fileSystem.CreateDirectory(_options.RootDirectory);

            _logger.LogDebug(
                "Health probe for file connector '{ConnectorName}': Healthy",
                Name);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Health probe for file connector '{ConnectorName}' failed", Name);
            return Task.FromResult(false);
        }
    }
}
