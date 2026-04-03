using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Connectors;

/// <summary>
/// Unified abstraction over all platform connectors (HTTP, SFTP, Email, File).
/// </summary>
/// <remarks>
/// <para>
/// Every concrete connector adapter implements this interface to provide a
/// common send/receive API regardless of the underlying transport. The
/// <see cref="IConnectorRegistry"/> and <see cref="IConnectorFactory"/> use this
/// interface for runtime resolution.
/// </para>
/// <para>
/// Connector implementations are expected to handle serialization, correlation
/// header propagation, and transport-specific concerns internally.
/// </para>
/// </remarks>
public interface IConnector
{
    /// <summary>The unique name of this connector instance.</summary>
    string Name { get; }

    /// <summary>The transport type of this connector.</summary>
    ConnectorType ConnectorType { get; }

    /// <summary>
    /// Sends an integration envelope through this connector's transport.
    /// </summary>
    /// <typeparam name="T">The envelope payload type.</typeparam>
    /// <param name="envelope">The message envelope to send.</param>
    /// <param name="options">
    /// Transport-specific options (e.g. relative URL for HTTP, filename for File/SFTP,
    /// recipient for Email). Implementations cast to their specific options type.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ConnectorResult"/> describing the outcome.</returns>
    Task<ConnectorResult> SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        ConnectorSendOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests whether the connector can reach its target system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> when the target is reachable; <see langword="false"/> otherwise.
    /// </returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
