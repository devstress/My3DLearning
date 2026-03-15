using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Storage.Cassandra;

/// <summary>
/// Creates the Cassandra keyspace and tables required by the platform.
/// Called once during session factory initialisation.
/// </summary>
internal static class SchemaManager
{
    /// <summary>
    /// Ensures the keyspace and all required tables exist.
    /// Uses <c>IF NOT EXISTS</c> to make the operation idempotent.
    /// </summary>
    /// <param name="session">A connected Cassandra session (no keyspace selected).</param>
    /// <param name="keyspace">The keyspace name.</param>
    /// <param name="replicationFactor">SimpleStrategy replication factor.</param>
    /// <param name="defaultTtlSeconds">Default TTL for tables; 0 disables TTL.</param>
    /// <param name="logger">Logger for schema creation events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    internal static async Task EnsureSchemaAsync(
        global::Cassandra.ISession session,
        string keyspace,
        int replicationFactor,
        int defaultTtlSeconds,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Ensuring Cassandra schema for keyspace {Keyspace} (RF={ReplicationFactor}, TTL={TtlSeconds}s)",
            keyspace, replicationFactor, defaultTtlSeconds);

        var createKeyspace = $@"
            CREATE KEYSPACE IF NOT EXISTS {keyspace}
            WITH replication = {{
                'class': 'SimpleStrategy',
                'replication_factor': {replicationFactor}
            }}";

        await session.ExecuteAsync(new global::Cassandra.SimpleStatement(createKeyspace));

        await session.ExecuteAsync(new global::Cassandra.SimpleStatement($"USE {keyspace}"));

        var ttlClause = defaultTtlSeconds > 0
            ? $"AND default_time_to_live = {defaultTtlSeconds}"
            : string.Empty;

        // ── Messages by correlation ID ────────────────────────────────────────
        // Partition: correlation_id. Clustering: recorded_at ASC, message_id ASC.
        // Supports efficient range scans for "show me all events for correlation X".
        var createMessagesByCorrelation = $@"
            CREATE TABLE IF NOT EXISTS messages_by_correlation_id (
                correlation_id uuid,
                message_id     uuid,
                causation_id   uuid,
                recorded_at    timestamp,
                source         text,
                message_type   text,
                schema_version text,
                priority       int,
                payload_json   text,
                metadata_json  text,
                delivery_status int,
                PRIMARY KEY (correlation_id, recorded_at, message_id)
            ) WITH CLUSTERING ORDER BY (recorded_at ASC, message_id ASC)
            {ttlClause}";

        await session.ExecuteAsync(new global::Cassandra.SimpleStatement(createMessagesByCorrelation));

        // ── Messages by message ID ────────────────────────────────────────────
        // Partition: message_id. Single-row lookup for "give me this specific message".
        var createMessagesById = $@"
            CREATE TABLE IF NOT EXISTS messages_by_id (
                message_id     uuid,
                correlation_id uuid,
                causation_id   uuid,
                recorded_at    timestamp,
                source         text,
                message_type   text,
                schema_version text,
                priority       int,
                payload_json   text,
                metadata_json  text,
                delivery_status int,
                PRIMARY KEY (message_id)
            ) {(defaultTtlSeconds > 0 ? $"WITH default_time_to_live = {defaultTtlSeconds}" : string.Empty)}";

        await session.ExecuteAsync(new global::Cassandra.SimpleStatement(createMessagesById));

        // ── Faults by correlation ID ──────────────────────────────────────────
        // Partition: correlation_id. Clustering: faulted_at DESC, fault_id ASC.
        // Supports "show me all faults for this business transaction" sorted newest first.
        var createFaultsByCorrelation = $@"
            CREATE TABLE IF NOT EXISTS faults_by_correlation_id (
                correlation_id       uuid,
                fault_id             uuid,
                original_message_id  uuid,
                original_message_type text,
                faulted_by           text,
                fault_reason         text,
                faulted_at           timestamp,
                retry_count          int,
                error_details        text,
                original_payload_json text,
                PRIMARY KEY (correlation_id, faulted_at, fault_id)
            ) WITH CLUSTERING ORDER BY (faulted_at DESC, fault_id ASC)
            {ttlClause}";

        await session.ExecuteAsync(new global::Cassandra.SimpleStatement(createFaultsByCorrelation));

        logger.LogInformation("Cassandra schema for keyspace {Keyspace} is ready", keyspace);
    }
}
