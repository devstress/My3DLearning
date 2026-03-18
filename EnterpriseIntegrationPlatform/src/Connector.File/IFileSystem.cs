namespace EnterpriseIntegrationPlatform.Connector.FileSystem;

/// <summary>
/// Abstraction over the physical file system, allowing the file connector to be
/// unit-tested without touching disk.
/// </summary>
public interface IFileSystem
{
    /// <summary>Writes all <paramref name="contents"/> to the file at <paramref name="path"/>.</summary>
    /// <param name="path">Absolute file path.</param>
    /// <param name="contents">Bytes to write.</param>
    /// <param name="ct">Cancellation token.</param>
    Task WriteAllBytesAsync(string path, byte[] contents, CancellationToken ct);

    /// <summary>Reads all bytes from the file at <paramref name="path"/>.</summary>
    /// <param name="path">Absolute file path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The file contents.</returns>
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct);

    /// <summary>Returns all file paths in <paramref name="directory"/> that match <paramref name="searchPattern"/>.</summary>
    /// <param name="directory">Directory to search.</param>
    /// <param name="searchPattern">Search pattern (e.g. <c>*.json</c>).</param>
    /// <returns>Matching file paths.</returns>
    IEnumerable<string> GetFiles(string directory, string searchPattern);

    /// <summary>Returns <c>true</c> if the file at <paramref name="path"/> exists.</summary>
    bool FileExists(string path);

    /// <summary>Creates the directory at <paramref name="path"/> and any missing parent directories.</summary>
    void CreateDirectory(string path);
}
