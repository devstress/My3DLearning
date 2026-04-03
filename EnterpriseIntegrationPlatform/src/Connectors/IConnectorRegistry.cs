namespace EnterpriseIntegrationPlatform.Connectors;

/// <summary>
/// Runtime registry of all available <see cref="IConnector"/> instances.
/// Supports registration, removal, and lookup by name or type.
/// </summary>
public interface IConnectorRegistry
{
    /// <summary>
    /// Registers a connector. If a connector with the same name exists, it is replaced.
    /// </summary>
    /// <param name="connector">The connector instance to register.</param>
    void Register(IConnector connector);

    /// <summary>
    /// Removes a connector by name.
    /// </summary>
    /// <param name="name">The unique connector name (case-insensitive).</param>
    /// <returns><see langword="true"/> if the connector was found and removed.</returns>
    bool Remove(string name);

    /// <summary>
    /// Retrieves a connector by name.
    /// </summary>
    /// <param name="name">The unique connector name (case-insensitive).</param>
    /// <returns>The connector if found; <see langword="null"/> otherwise.</returns>
    IConnector? GetByName(string name);

    /// <summary>
    /// Returns all registered connectors of the specified type.
    /// </summary>
    /// <param name="connectorType">The transport type to filter by.</param>
    /// <returns>All connectors matching the type.</returns>
    IReadOnlyList<IConnector> GetByType(ConnectorType connectorType);

    /// <summary>Returns all registered connectors.</summary>
    IReadOnlyList<IConnector> GetAll();

    /// <summary>Returns descriptors for all registered connectors.</summary>
    IReadOnlyList<ConnectorDescriptor> GetDescriptors();

    /// <summary>Returns the total number of registered connectors.</summary>
    int Count { get; }
}
