namespace EnterpriseIntegrationPlatform.Storage.Cassandra;

/// <summary>
/// Configuration options for connecting to a Cassandra cluster.
/// Bind from the <c>Cassandra</c> section in <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
/// </summary>
public sealed class CassandraOptions
{
    /// <summary>Configuration section name used for binding.</summary>
    public const string SectionName = "Cassandra";

    /// <summary>
    /// Comma-separated list of contact point addresses (hostnames or IPs).
    /// Defaults to <c>localhost</c> for local development.
    /// </summary>
    public string ContactPoints { get; set; } = "localhost";

    /// <summary>
    /// CQL native transport port. Defaults to <c>15042</c> to match the
    /// Aspire host port mapping (15xxx range avoids conflicts).
    /// </summary>
    public int Port { get; set; } = 15042;

    /// <summary>
    /// Keyspace name. Created automatically by <see cref="SchemaManager"/> if it
    /// does not exist.
    /// </summary>
    public string Keyspace { get; set; } = "eip";

    /// <summary>
    /// Replication factor for the keyspace. Defaults to <c>3</c> for production
    /// durability (RF=3 satisfies Quality Pillar 1 – Reliability).
    /// </summary>
    public int ReplicationFactor { get; set; } = 3;

    /// <summary>
    /// Default time-to-live in seconds for data rows. Set to <c>0</c> to disable
    /// TTL-based cleanup. Defaults to 30 days (2,592,000 seconds).
    /// </summary>
    public int DefaultTtlSeconds { get; set; } = 2_592_000;
}
