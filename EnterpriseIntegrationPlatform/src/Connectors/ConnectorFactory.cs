using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Connectors;

/// <summary>
/// Production implementation of <see cref="IConnectorFactory"/>.
/// Resolves connectors from the <see cref="IConnectorRegistry"/>.
/// </summary>
public sealed class ConnectorFactory : IConnectorFactory
{
    private readonly IConnectorRegistry _registry;
    private readonly ILogger<ConnectorFactory> _logger;

    /// <summary>Initialises a new instance of <see cref="ConnectorFactory"/>.</summary>
    public ConnectorFactory(
        IConnectorRegistry registry,
        ILogger<ConnectorFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _logger = logger;
    }

    /// <inheritdoc />
    public IConnector Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var connector = _registry.GetByName(name);
        if (connector is null)
        {
            _logger.LogError("Connector '{ConnectorName}' not found in registry", name);
            throw new InvalidOperationException(
                $"No connector named '{name}' is registered. " +
                $"Available: [{string.Join(", ", _registry.GetAll().Select(c => c.Name))}]");
        }

        _logger.LogDebug("Resolved connector '{ConnectorName}' ({ConnectorType})",
            connector.Name, connector.ConnectorType);

        return connector;
    }

    /// <inheritdoc />
    public bool TryCreate(string name, out IConnector? connector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        connector = _registry.GetByName(name);
        return connector is not null;
    }
}
