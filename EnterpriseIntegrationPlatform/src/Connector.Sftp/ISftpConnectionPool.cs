namespace EnterpriseIntegrationPlatform.Connector.Sftp;

/// <summary>
/// Manages a pool of <see cref="ISftpClient"/> connections keyed by host, reusing
/// connections across requests to amortise the cost of TCP + SSH negotiation.
/// </summary>
public interface ISftpConnectionPool : IAsyncDisposable
{
    /// <summary>
    /// Acquires a connected <see cref="ISftpClient"/> from the pool. If no idle
    /// connection is available and the pool for the host is not yet at capacity,
    /// a new connection is created. Otherwise the call blocks until a connection
    /// is returned.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A connected SFTP client that must be returned via <see cref="Release"/>.</returns>
    Task<ISftpClient> AcquireAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a previously acquired connection to the pool so it can be reused.
    /// If the connection is no longer valid it is disposed instead of being pooled.
    /// </summary>
    /// <param name="client">The client to return.</param>
    void Release(ISftpClient client);
}
