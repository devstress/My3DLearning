using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Connector.FileSystem;

/// <summary>
/// Reads and writes files on the local (or network) file system on behalf of the integration platform.
/// </summary>
public interface IFileConnector
{
    /// <summary>
    /// Serializes the envelope payload and writes it to a file whose name is derived from
    /// <see cref="FileConnectorOptions.FilenamePattern"/>. A JSON metadata sidecar is
    /// written alongside the data file.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope whose payload will be written.</param>
    /// <param name="serializer">Function that converts the payload to bytes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The full path of the written file.</returns>
    Task<string> WriteAsync<T>(
        IntegrationEnvelope<T> envelope,
        Func<T, byte[]> serializer,
        CancellationToken ct);

    /// <summary>Reads and returns the raw bytes from the file at <paramref name="filePath"/>.</summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>File contents as a byte array.</returns>
    Task<byte[]> ReadAsync(string filePath, CancellationToken ct);

    /// <summary>
    /// Lists all files in <see cref="FileConnectorOptions.RootDirectory"/> (or an optional
    /// subdirectory) that match the given search pattern.
    /// </summary>
    /// <param name="subdirectory">
    /// Optional subdirectory relative to <see cref="FileConnectorOptions.RootDirectory"/>.
    /// Pass <c>null</c> to list files directly in the root directory.
    /// </param>
    /// <param name="searchPattern">File search pattern (e.g. <c>*.json</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of matching file paths.</returns>
    Task<IReadOnlyList<string>> ListFilesAsync(
        string? subdirectory,
        string searchPattern,
        CancellationToken ct);
}
