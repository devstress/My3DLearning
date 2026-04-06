// ============================================================================
// MockFileSystem – In-memory file system for testing
// ============================================================================

using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Connector.FileSystem;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="IFileSystem"/> backed by
/// a dictionary-based store.
/// </summary>
public sealed class MockFileSystem : IFileSystem
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();
    private readonly ConcurrentQueue<FileSystemCallRecord> _calls = new();

    /// <summary>All calls recorded.</summary>
    public IReadOnlyList<FileSystemCallRecord> Calls => _calls.ToArray();

    /// <summary>In-memory file store — pre-populate for read tests.</summary>
    public ConcurrentDictionary<string, byte[]> Files => _files;

    /// <summary>Gets the last written file path (excluding .meta files).</summary>
    public string? LastWrittenPath =>
        _calls.LastOrDefault(c => c.Operation == "WriteAllBytes" && !c.Path!.EndsWith(".meta.json"))?.Path;

    public Task WriteAllBytesAsync(string path, byte[] contents, CancellationToken ct)
    {
        _files[path] = contents;
        _calls.Enqueue(new FileSystemCallRecord("WriteAllBytes", path));
        return Task.CompletedTask;
    }

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
    {
        _calls.Enqueue(new FileSystemCallRecord("ReadAllBytes", path));
        if (_files.TryGetValue(path, out var data))
            return Task.FromResult(data);
        throw new FileNotFoundException($"File not found: {path}");
    }

    public IEnumerable<string> GetFiles(string directory, string searchPattern)
    {
        _calls.Enqueue(new FileSystemCallRecord("GetFiles", directory));
        return _files.Keys
            .Where(k => k.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public bool FileExists(string path)
    {
        _calls.Enqueue(new FileSystemCallRecord("FileExists", path));
        return _files.ContainsKey(path);
    }

    public void CreateDirectory(string path)
    {
        _calls.Enqueue(new FileSystemCallRecord("CreateDirectory", path));
    }

    public void Reset()
    {
        _files.Clear();
        while (_calls.TryDequeue(out _)) { }
    }

    public sealed record FileSystemCallRecord(string Operation, string? Path);
}
