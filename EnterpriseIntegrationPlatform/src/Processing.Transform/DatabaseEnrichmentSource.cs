using System.Data;
using System.Data.Common;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Enrichment source that fetches data from a database using a parameterised SQL query.
/// </summary>
/// <remarks>
/// The query must return a single row. Each column is mapped to a JSON property
/// on the returned <see cref="JsonObject"/>.
/// </remarks>
public sealed class DatabaseEnrichmentSource : IEnrichmentSource
{
    private readonly Func<DbConnection> _connectionFactory;
    private readonly string _sql;
    private readonly string _parameterName;
    private readonly ILogger<DatabaseEnrichmentSource> _logger;

    /// <summary>Initialises a new instance of <see cref="DatabaseEnrichmentSource"/>.</summary>
    /// <param name="connectionFactory">Factory that creates a new <see cref="DbConnection"/>.</param>
    /// <param name="sql">Parameterised SQL query (e.g. <c>SELECT name, tier FROM customers WHERE id = @key</c>).</param>
    /// <param name="parameterName">Name of the SQL parameter for the lookup key (e.g. <c>@key</c>).</param>
    /// <param name="logger">Logger instance.</param>
    public DatabaseEnrichmentSource(
        Func<DbConnection> connectionFactory,
        string sql,
        string parameterName,
        ILogger<DatabaseEnrichmentSource> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        ArgumentNullException.ThrowIfNull(logger);

        _connectionFactory = connectionFactory;
        _sql = sql;
        _parameterName = parameterName;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<JsonNode?> FetchAsync(string lookupKey, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = _sql;

        var param = command.CreateParameter();
        param.ParameterName = _parameterName;
        param.Value = lookupKey;
        command.Parameters.Add(param);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            _logger.LogDebug("Database enrichment: no rows for key '{Key}'", lookupKey);
            return null;
        }

        var obj = new JsonObject();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            var value = reader.IsDBNull(i) ? null : reader.GetValue(i)?.ToString();
            obj[name] = value is not null ? JsonValue.Create(value) : null;
        }

        _logger.LogDebug("Database enrichment: found row for key '{Key}'", lookupKey);
        return obj;
    }
}
