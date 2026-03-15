namespace EnterpriseIntegrationPlatform.Connectors.File;

/// <summary>
/// Reads and writes files on a local or network file system.
/// </summary>
public interface IFileConnector
{
    /// <summary>Reads the contents of a file.</summary>
    Task<byte[]> ReadAsync(string path, CancellationToken ct = default);

    /// <summary>Writes content to a file, creating or overwriting it.</summary>
    Task WriteAsync(string path, byte[] content, CancellationToken ct = default);

    /// <summary>Lists files in a directory matching the given pattern.</summary>
    Task<IReadOnlyList<string>> ListAsync(
        string directory,
        string searchPattern = "*",
        CancellationToken ct = default);

    /// <summary>Deletes a file at the specified path.</summary>
    Task DeleteAsync(string path, CancellationToken ct = default);
}
