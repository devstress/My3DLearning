using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Connector.Sftp;

/// <summary>
/// Thread-safe connection pool that maintains up to
/// <see cref="SftpConnectorOptions.MaxConnectionsPerHost"/> SFTP connections for the
/// configured host. Idle connections exceeding
/// <see cref="SftpConnectorOptions.ConnectionIdleTimeoutMs"/> are evicted on acquire.
/// </summary>
public sealed class SftpConnectionPool : ISftpConnectionPool
{
    private readonly Func<ISftpClient> _clientFactory;
    private readonly int _maxConnections;
    private readonly TimeSpan _idleTimeout;
    private readonly ILogger<SftpConnectionPool> _logger;

    // Bounded channel acts as a semaphore + queue: we pre-fill it with _maxConnections
    // "slots". Acquiring pops a slot; releasing pushes one back.
    private readonly Channel<byte> _semaphore;

    // Idle connections waiting to be reused.
    private readonly ConcurrentQueue<PooledConnection> _idle = new();

    private volatile bool _disposed;

    /// <summary>Initialises a new pool for the configured host.</summary>
    public SftpConnectionPool(
        Func<ISftpClient> clientFactory,
        IOptions<SftpConnectorOptions> options,
        ILogger<SftpConnectionPool> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var opts = options.Value;
        _clientFactory = clientFactory;
        _maxConnections = Math.Max(opts.MaxConnectionsPerHost, 1);
        _idleTimeout = TimeSpan.FromMilliseconds(
            opts.ConnectionIdleTimeoutMs > 0 ? opts.ConnectionIdleTimeoutMs : int.MaxValue);
        _logger = logger;

        _semaphore = Channel.CreateBounded<byte>(new BoundedChannelOptions(_maxConnections)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
        });

        // Pre-fill the semaphore to represent available capacity.
        for (var i = 0; i < _maxConnections; i++)
            _semaphore.Writer.TryWrite(0);
    }

    /// <inheritdoc />
    public async Task<ISftpClient> AcquireAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Wait for a free slot (blocks if pool is at capacity).
        await _semaphore.Reader.ReadAsync(ct).ConfigureAwait(false);

        // Try to reuse an idle connection.
        while (_idle.TryDequeue(out var pooled))
        {
            if (IsExpired(pooled))
            {
                DisposeClient(pooled.Client);
                continue;
            }

            if (pooled.Client.IsConnected)
            {
                _logger.LogDebug("Reusing pooled SFTP connection");
                return pooled.Client;
            }

            DisposeClient(pooled.Client);
        }

        // Create a new connection.
        var client = _clientFactory();
        client.Connect();
        _logger.LogDebug("Created new SFTP connection (pool capacity {Max})", _maxConnections);
        return client;
    }

    /// <inheritdoc />
    public void Release(ISftpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (_disposed || !client.IsConnected)
        {
            DisposeClient(client);
            _semaphore.Writer.TryWrite(0); // return the slot
            return;
        }

        _idle.Enqueue(new PooledConnection(client, DateTimeOffset.UtcNow));
        _semaphore.Writer.TryWrite(0); // return the slot

        _logger.LogDebug("Returned SFTP connection to pool");
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        _disposed = true;

        while (_idle.TryDequeue(out var pooled))
            DisposeClient(pooled.Client);

        _semaphore.Writer.TryComplete();

        _logger.LogDebug("SFTP connection pool disposed");
        return ValueTask.CompletedTask;
    }

    private bool IsExpired(PooledConnection pooled)
        => DateTimeOffset.UtcNow - pooled.ReturnedAt > _idleTimeout;

    private void DisposeClient(ISftpClient client)
    {
        try
        {
            if (client.IsConnected)
                client.Disconnect();

            (client as IDisposable)?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing pooled SFTP connection");
        }
    }

    private readonly record struct PooledConnection(ISftpClient Client, DateTimeOffset ReturnedAt);
}
