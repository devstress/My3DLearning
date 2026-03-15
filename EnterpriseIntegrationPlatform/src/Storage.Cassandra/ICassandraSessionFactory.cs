using Cassandra;

namespace EnterpriseIntegrationPlatform.Storage.Cassandra;

/// <summary>
/// Factory that provides a connected <see cref="ISession"/> for the configured
/// Cassandra keyspace. Implementations manage the underlying <see cref="Cluster"/>
/// lifecycle and ensure the schema is created before the first session is returned.
/// </summary>
public interface ICassandraSessionFactory : IAsyncDisposable
{
    /// <summary>
    /// Returns a connected <see cref="ISession"/> for the configured keyspace.
    /// The session is reused across calls (singleton per factory instance).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A connected Cassandra session.</returns>
    Task<ISession> GetSessionAsync(CancellationToken cancellationToken = default);
}
