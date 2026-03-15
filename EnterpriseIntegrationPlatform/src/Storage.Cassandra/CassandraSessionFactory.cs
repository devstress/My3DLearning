using System.Diagnostics;
using Cassandra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Storage.Cassandra;

/// <summary>
/// Creates and manages a Cassandra <see cref="Cluster"/> and <see cref="ISession"/>.
/// Ensures the keyspace and tables are created on first use via <see cref="SchemaManager"/>.
/// Thread-safe; the session is lazily initialised and reused across all callers.
/// </summary>
public sealed class CassandraSessionFactory : ICassandraSessionFactory
{
    private readonly CassandraOptions _options;
    private readonly ILogger<CassandraSessionFactory> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private Cluster? _cluster;
    private ISession? _session;
    private bool _disposed;

    /// <summary>
    /// Initialises a new instance of <see cref="CassandraSessionFactory"/>.
    /// </summary>
    /// <param name="options">Cassandra connection options.</param>
    /// <param name="logger">Logger.</param>
    public CassandraSessionFactory(
        IOptions<CassandraOptions> options,
        ILogger<CassandraSessionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ISession> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_session is not null)
        {
            return _session;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_session is not null)
            {
                return _session;
            }

            var contactPoints = _options.ContactPoints
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            _logger.LogInformation(
                "Connecting to Cassandra cluster at {ContactPoints}:{Port}, keyspace={Keyspace}",
                _options.ContactPoints, _options.Port, _options.Keyspace);

            _cluster = Cluster.Builder()
                .AddContactPoints(contactPoints)
                .WithPort(_options.Port)
                .WithQueryOptions(new QueryOptions().SetConsistencyLevel(ConsistencyLevel.LocalQuorum))
                .Build();

            // Connect without keyspace first to create schema
            var systemSession = await _cluster.ConnectAsync();
            await SchemaManager.EnsureSchemaAsync(
                systemSession,
                _options.Keyspace,
                _options.ReplicationFactor,
                _options.DefaultTtlSeconds,
                _logger,
                cancellationToken);

            _session = await _cluster.ConnectAsync(_options.Keyspace);

            _logger.LogInformation(
                "Connected to Cassandra keyspace {Keyspace}",
                _options.Keyspace);

            return _session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Cassandra cluster at {ContactPoints}:{Port}",
                _options.ContactPoints, _options.Port);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _session?.Dispose();
        if (_cluster is not null)
        {
            await _cluster.ShutdownAsync();
        }

        _semaphore.Dispose();
    }
}
