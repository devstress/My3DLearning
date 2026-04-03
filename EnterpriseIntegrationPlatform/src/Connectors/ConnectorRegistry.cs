using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.Connectors;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IConnectorRegistry"/>.
/// </summary>
public sealed class ConnectorRegistry : IConnectorRegistry
{
    private readonly ConcurrentDictionary<string, IConnector> _connectors =
        new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void Register(IConnector connector)
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentException.ThrowIfNullOrWhiteSpace(connector.Name);

        _connectors[connector.Name] = connector;
    }

    /// <inheritdoc />
    public bool Remove(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _connectors.TryRemove(name, out _);
    }

    /// <inheritdoc />
    public IConnector? GetByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _connectors.TryGetValue(name, out var connector) ? connector : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<IConnector> GetByType(ConnectorType connectorType) =>
        [.. _connectors.Values.Where(c => c.ConnectorType == connectorType)];

    /// <inheritdoc />
    public IReadOnlyList<IConnector> GetAll() =>
        [.. _connectors.Values];

    /// <inheritdoc />
    public IReadOnlyList<ConnectorDescriptor> GetDescriptors() =>
        [.. _connectors.Values.Select(c => new ConnectorDescriptor
        {
            Name = c.Name,
            ConnectorType = c.ConnectorType,
            ImplementationType = c.GetType(),
        })];

    /// <inheritdoc />
    public int Count => _connectors.Count;
}
