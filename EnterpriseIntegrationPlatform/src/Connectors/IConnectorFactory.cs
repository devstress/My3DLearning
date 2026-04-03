namespace EnterpriseIntegrationPlatform.Connectors;

/// <summary>
/// Factory for resolving <see cref="IConnector"/> instances by name.
/// Uses the <see cref="IConnectorRegistry"/> for lookup.
/// </summary>
public interface IConnectorFactory
{
    /// <summary>
    /// Resolves a connector by name.
    /// </summary>
    /// <param name="name">The unique connector name (case-insensitive).</param>
    /// <returns>The connector.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no connector with the given name is registered.
    /// </exception>
    IConnector Create(string name);

    /// <summary>
    /// Attempts to resolve a connector by name.
    /// </summary>
    /// <param name="name">The unique connector name (case-insensitive).</param>
    /// <param name="connector">The resolved connector, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the connector was found.</returns>
    bool TryCreate(string name, out IConnector? connector);
}
