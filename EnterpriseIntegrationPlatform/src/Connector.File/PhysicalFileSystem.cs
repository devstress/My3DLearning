namespace EnterpriseIntegrationPlatform.Connector.FileSystem;

/// <summary>
/// Production <see cref="IFileSystem"/> implementation that delegates to
/// <see cref="System.IO.File"/> and <see cref="System.IO.Directory"/>.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    /// <inheritdoc />
    public Task WriteAllBytesAsync(string path, byte[] contents, CancellationToken ct) =>
        System.IO.File.WriteAllBytesAsync(path, contents, ct);

    /// <inheritdoc />
    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct) =>
        System.IO.File.ReadAllBytesAsync(path, ct);

    /// <inheritdoc />
    public IEnumerable<string> GetFiles(string directory, string searchPattern) =>
        System.IO.Directory.GetFiles(directory, searchPattern);

    /// <inheritdoc />
    public bool FileExists(string path) =>
        System.IO.File.Exists(path);

    /// <inheritdoc />
    public void CreateDirectory(string path) =>
        System.IO.Directory.CreateDirectory(path);
}
