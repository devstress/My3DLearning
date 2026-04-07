using Npgsql;

namespace EnterpriseIntegrationPlatform.Ingestion.Postgres;

/// <summary>
/// Factory for creating Npgsql connections to the EIP Postgres message broker.
/// Manages the connection string and provides schema initialization.
/// </summary>
public sealed class PostgresConnectionFactory : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly NpgsqlDataSource _dataSource;

    public PostgresConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
        _dataSource = NpgsqlDataSource.Create(connectionString);
    }

    /// <summary>
    /// Opens a new connection from the pool.
    /// </summary>
    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = _dataSource.CreateConnection();
        await conn.OpenAsync(ct);
        return conn;
    }

    /// <summary>
    /// The underlying data source (connection pool).
    /// </summary>
    public NpgsqlDataSource DataSource => _dataSource;

    /// <summary>
    /// Initializes the EIP schema by executing the embedded SQL migration.
    /// Idempotent — safe to call on every startup.
    /// </summary>
    public async Task InitializeSchemaAsync(CancellationToken ct = default)
    {
        var sql = GetEmbeddedSchema();
        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Reads the schema SQL from the embedded resource or file.
    /// </summary>
    internal static string GetEmbeddedSchema()
    {
        // Read from the Schema folder relative to the assembly
        var assembly = typeof(PostgresConnectionFactory).Assembly;
        var resourceName = "Ingestion.Postgres.Schema.001_create_tables.sql";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is not null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // Fallback: read from file path (development/test scenarios)
        var dir = Path.GetDirectoryName(assembly.Location)!;
        var filePath = Path.Combine(dir, "Schema", "001_create_tables.sql");
        if (File.Exists(filePath))
            return File.ReadAllText(filePath);

        throw new InvalidOperationException(
            "Could not find EIP Postgres schema. Ensure 001_create_tables.sql " +
            "is included as an embedded resource or copied to the output directory.");
    }

    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
    }
}
