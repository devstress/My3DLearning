// ============================================================================
// AspireFixture – Shared Aspire-hosted infrastructure for all tutorials
// ============================================================================
// Provides real NATS JetStream, Temporal, SFTP, and MailHog services via
// the Aspire TestAppHost. Tests that need a real message broker create a
// NatsBrokerEndpoint from the NATS URL. Tests that need Temporal use the
// real Temporal address.
//
// This [SetUpFixture] is in the global (no) namespace so it applies to ALL
// test classes in the assembly. It starts once per test run.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;

/// <summary>
/// NUnit SetUpFixture that starts the Aspire TestAppHost once per test run.
/// All tutorials share the same infrastructure containers.
/// Placed in the global namespace so it applies to all test fixtures.
/// </summary>
[SetUpFixture]
public sealed class AspireFixture
{
    /// <summary>Whether Aspire infrastructure is available (Docker running).</summary>
    public static bool IsAvailable { get; private set; }

    /// <summary>NATS connection URL from the running Aspire TestAppHost.</summary>
    public static string? NatsUrl { get; private set; }

    /// <summary>Temporal gRPC address from the running Aspire TestAppHost.</summary>
    public static string? TemporalAddress { get; private set; }

    /// <summary>SFTP endpoint from the running Aspire TestAppHost.</summary>
    public static (string Host, int Port)? SftpEndpoint { get; private set; }

    /// <summary>SMTP endpoint from the running Aspire TestAppHost.</summary>
    public static (string Host, int SmtpPort, int ApiPort)? SmtpEndpoint { get; private set; }

    /// <summary>PostgreSQL connection string from the running Aspire TestAppHost.</summary>
    public static string? PostgresConnectionString { get; private set; }

    /// <summary>Kafka bootstrap servers from the running Aspire TestAppHost.</summary>
    public static string? KafkaBootstrapServers { get; private set; }

    /// <summary>Pulsar service URL from the running Aspire TestAppHost.</summary>
    public static string? PulsarServiceUrl { get; private set; }

    [OneTimeSetUp]
    public async Task GlobalSetUp()
    {
        var app = await SharedTestAppHost.GetAppAsync();
        IsAvailable = app is not null;

        if (IsAvailable)
        {
            // Fast lookups (containers already healthy or instant endpoint resolution)
            NatsUrl = await SharedTestAppHost.GetNatsUrlAsync();
            TemporalAddress = await SharedTestAppHost.GetTemporalAddressAsync();
            SftpEndpoint = await SharedTestAppHost.GetSftpEndpointAsync();
            SmtpEndpoint = await SharedTestAppHost.GetSmtpEndpointAsync();

            // Slow container readiness probes — run in parallel to reduce startup time.
            // Kafka ~30s, Pulsar ~60-120s, Postgres ~10s. Sequential = ~200s. Parallel = ~120s.
            var pgTask = SharedTestAppHost.GetPostgresConnectionStringAsync();
            var kafkaTask = SharedTestAppHost.GetKafkaBootstrapServersAsync();
            var pulsarTask = SharedTestAppHost.GetPulsarServiceUrlAsync();
            await Task.WhenAll(pgTask, kafkaTask, pulsarTask);

            PostgresConnectionString = pgTask.Result;
            KafkaBootstrapServers = kafkaTask.Result;
            PulsarServiceUrl = pulsarTask.Result;
        }
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        await SharedTestAppHost.DisposeAsync();
    }

    /// <summary>
    /// Creates a NatsBrokerEndpoint connected to the real NATS JetStream.
    /// Throws Fail if Docker is not available.
    /// </summary>
    public static NatsBrokerEndpoint CreateNatsEndpoint(string name)
    {
        if (!IsAvailable || NatsUrl is null)
        {
            Assert.Fail("Docker not available — skipping real broker test");
            return null!; // unreachable — Assert.Fail throws
        }
        return new NatsBrokerEndpoint(name, NatsUrl);
    }

    /// <summary>
    /// Creates a PostgresBrokerEndpoint connected to the real PostgreSQL.
    /// Throws Fail if Docker is not available.
    /// </summary>
    public static PostgresBrokerEndpoint CreatePostgresEndpoint(string name)
    {
        if (!IsAvailable || PostgresConnectionString is null)
        {
            Assert.Fail("Docker not available — skipping real Postgres broker test");
            return null!; // unreachable — Assert.Fail throws
        }
        return new PostgresBrokerEndpoint(name, PostgresConnectionString);
    }

    /// <summary>
    /// Creates a KafkaBrokerEndpoint connected to the real Apache Kafka.
    /// Throws Fail if Docker is not available.
    /// </summary>
    public static KafkaBrokerEndpoint CreateKafkaEndpoint(string name)
    {
        if (!IsAvailable || KafkaBootstrapServers is null)
        {
            Assert.Fail("Docker not available — skipping real Kafka broker test");
            return null!; // unreachable — Assert.Fail throws
        }
        return new KafkaBrokerEndpoint(name, KafkaBootstrapServers);
    }

    /// <summary>
    /// Creates a PulsarBrokerEndpoint connected to the real Apache Pulsar.
    /// Throws Fail if Docker is not available.
    /// </summary>
    public static PulsarBrokerEndpoint CreatePulsarEndpoint(string name)
    {
        if (!IsAvailable || PulsarServiceUrl is null)
        {
            Assert.Fail("Docker not available — skipping real Pulsar broker test");
            return null!; // unreachable — Assert.Fail throws
        }
        return new PulsarBrokerEndpoint(name, PulsarServiceUrl);
    }

    /// <summary>
    /// Generates a unique topic name to prevent cross-test interference.
    /// </summary>
    public static string UniqueTopic(string prefix = "test") =>
        $"{prefix}-{Guid.NewGuid():N}";
}
