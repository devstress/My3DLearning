// ============================================================================
// MockSftpClient + MockSftpConnectionPool – In-memory SFTP for testing
// ============================================================================

using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Connector.Sftp;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="ISftpClient"/> backed by a
/// dictionary-based file store.
/// </summary>
public sealed class MockSftpClient : ISftpClient
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();
    private readonly ConcurrentQueue<SftpCallRecord> _calls = new();
    private bool _connected;

    /// <summary>All calls recorded against this client.</summary>
    public IReadOnlyList<SftpCallRecord> Calls => _calls.ToArray();

    /// <summary>Number of calls recorded.</summary>
    public int CallCount => _calls.Count;

    /// <summary>In-memory file store — pre-populate for download tests.</summary>
    public ConcurrentDictionary<string, byte[]> Files => _files;

    public bool IsConnected => _connected;

    public void Connect()
    {
        _connected = true;
        _calls.Enqueue(new SftpCallRecord("Connect", null));
    }

    public void Disconnect()
    {
        _connected = false;
        _calls.Enqueue(new SftpCallRecord("Disconnect", null));
    }

    public void UploadFile(Stream input, string remotePath)
    {
        using var ms = new MemoryStream();
        input.CopyTo(ms);
        _files[remotePath] = ms.ToArray();
        _calls.Enqueue(new SftpCallRecord("UploadFile", remotePath));
    }

    public Stream DownloadFile(string remotePath)
    {
        _calls.Enqueue(new SftpCallRecord("DownloadFile", remotePath));
        if (_files.TryGetValue(remotePath, out var data))
            return new MemoryStream(data);
        throw new FileNotFoundException($"SFTP file not found: {remotePath}");
    }

    public IEnumerable<string> ListFiles(string remotePath)
    {
        _calls.Enqueue(new SftpCallRecord("ListFiles", remotePath));
        return _files.Keys
            .Where(k => k.StartsWith(remotePath, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public void DeleteFile(string remotePath)
    {
        _calls.Enqueue(new SftpCallRecord("DeleteFile", remotePath));
        _files.TryRemove(remotePath, out _);
    }

    /// <summary>Returns the number of UploadFile calls.</summary>
    public int UploadCount => _calls.Count(c => c.Operation == "UploadFile");

    /// <summary>Returns uploaded file paths.</summary>
    public IReadOnlyList<string> UploadedPaths =>
        _calls.Where(c => c.Operation == "UploadFile")
            .Select(c => c.Path!)
            .ToList();

    public void Reset()
    {
        _files.Clear();
        while (_calls.TryDequeue(out _)) { }
        _connected = false;
    }

    public sealed record SftpCallRecord(string Operation, string? Path);
}

/// <summary>
/// Real in-memory implementation of <see cref="ISftpConnectionPool"/> that
/// wraps a <see cref="MockSftpClient"/>.
/// </summary>
public sealed class MockSftpConnectionPool : ISftpConnectionPool
{
    private readonly MockSftpClient _client;
    private int _acquireCount;
    private int _releaseCount;

    public MockSftpConnectionPool(MockSftpClient client) => _client = client;

    public MockSftpClient Client => _client;

    public int AcquireCount => _acquireCount;
    public int ReleaseCount => _releaseCount;

    public Task<ISftpClient> AcquireAsync(CancellationToken ct = default)
    {
        Interlocked.Increment(ref _acquireCount);
        _client.Connect();
        return Task.FromResult<ISftpClient>(_client);
    }

    public void Release(ISftpClient client)
    {
        Interlocked.Increment(ref _releaseCount);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void Reset()
    {
        _acquireCount = 0;
        _releaseCount = 0;
        _client.Reset();
    }
}

/// <summary>
/// Real in-memory implementation of <see cref="ISftpConnector"/>.
/// </summary>
public sealed class MockSftpConnector : ISftpConnector
{
    private readonly MockSftpClient _client;
    private readonly ConcurrentQueue<SftpConnectorCallRecord> _calls = new();

    public MockSftpConnector(MockSftpClient client) => _client = client;

    public IReadOnlyList<SftpConnectorCallRecord> Calls => _calls.ToArray();

    public Task<string> UploadAsync<T>(
        IntegrationEnvelope<T> envelope,
        string fileName,
        Func<T, byte[]> serializer,
        CancellationToken ct)
    {
        var bytes = serializer(envelope.Payload);
        var remotePath = $"/upload/{fileName}";
        _client.UploadFile(new MemoryStream(bytes), remotePath);
        _calls.Enqueue(new SftpConnectorCallRecord("Upload", remotePath));
        return Task.FromResult(remotePath);
    }

    public Task<byte[]> DownloadAsync(string remotePath, CancellationToken ct)
    {
        _calls.Enqueue(new SftpConnectorCallRecord("Download", remotePath));
        using var stream = _client.DownloadFile(remotePath);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Task.FromResult(ms.ToArray());
    }

    public Task<IReadOnlyList<string>> ListFilesAsync(string remotePath, CancellationToken ct)
    {
        _calls.Enqueue(new SftpConnectorCallRecord("ListFiles", remotePath));
        var files = _client.ListFiles(remotePath).ToList();
        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    public sealed record SftpConnectorCallRecord(string Operation, string Path);
}
