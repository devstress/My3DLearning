// ============================================================================
// SharedTestAppHost – Starts the TestAppHost via Aspire.Hosting.Testing
// ============================================================================
// Uses DistributedApplicationTestingBuilder to start the exact same Aspire
// infrastructure that production uses, ensuring tests match real deployments.
// Lazy-initialized: containers start only when first accessed.
// ============================================================================

using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace TutorialLabs.Infrastructure;

/// <summary>
/// Lazy-initialized Aspire test host backed by <c>TestAppHost</c>.
/// Starts real NATS JetStream, Temporal, SFTP, and MailHog containers
/// via the same Aspire orchestration used in production.
/// </summary>
public static class SharedTestAppHost
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DistributedApplication? _app;
    private static bool _attempted;

    /// <summary>Whether the Aspire test host started successfully.</summary>
    public static bool IsAvailable => _app is not null;

    /// <summary>
    /// Gets the Aspire distributed application, starting it if needed.
    /// Returns null when Docker is unavailable.
    /// </summary>
    public static async Task<DistributedApplication?> GetAppAsync()
    {
        if (_app is not null) return _app;
        if (_attempted) return null;

        await Gate.WaitAsync();
        try
        {
            if (_app is not null) return _app;
            if (_attempted) return null;
            _attempted = true;

            var appHost = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.TestAppHost>();

            _app = await appHost.BuildAsync();
            await _app.StartAsync();

            // Wait for NATS to be ready
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await _app.ResourceNotifications
                .WaitForResourceHealthyAsync("nats", cts.Token);

            return _app;
        }
        catch (Exception)
        {
            _app = null;
            return null;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>Gets the NATS connection URL from the running TestAppHost.</summary>
    public static async Task<string?> GetNatsUrlAsync()
    {
        var app = await GetAppAsync();
        if (app is null) return null;

        var natsEndpoint = app.GetEndpoint("nats", "nats-client");
        return natsEndpoint.ToString();
    }

    /// <summary>Gets the Temporal gRPC address from the running TestAppHost.</summary>
    public static async Task<string?> GetTemporalAddressAsync()
    {
        var app = await GetAppAsync();
        if (app is null) return null;

        var endpoint = app.GetEndpoint("temporal", "temporal-grpc");
        return $"{endpoint.Host}:{endpoint.Port}";
    }

    /// <summary>Gets the SFTP endpoint (host, port) from the running TestAppHost.</summary>
    public static async Task<(string Host, int Port)?> GetSftpEndpointAsync()
    {
        var app = await GetAppAsync();
        if (app is null) return null;

        var endpoint = app.GetEndpoint("sftp", "sftp-ssh");
        return (endpoint.Host, endpoint.Port);
    }

    /// <summary>Gets the MailHog SMTP endpoint from the running TestAppHost.</summary>
    public static async Task<(string Host, int SmtpPort, int ApiPort)?> GetSmtpEndpointAsync()
    {
        var app = await GetAppAsync();
        if (app is null) return null;

        var smtpEndpoint = app.GetEndpoint("mailhog", "smtp");
        var apiEndpoint = app.GetEndpoint("mailhog", "mailhog-api");
        return (smtpEndpoint.Host, smtpEndpoint.Port, apiEndpoint.Port);
    }

    /// <summary>Gets the PostgreSQL connection string from the running TestAppHost.</summary>
    /// <remarks>
    /// Waits for the Postgres container to accept connections before returning.
    /// Returns <c>null</c> when Docker/Postgres is unavailable so tests can skip.
    /// </remarks>
    public static async Task<string?> GetPostgresConnectionStringAsync()
    {
        var app = await GetAppAsync();
        if (app is null) return null;

        try
        {
            var endpoint = app.GetEndpoint("postgres", "postgres-tcp");
            var connStr = $"Host={endpoint.Host};Port={endpoint.Port};Database=eip;Username=eip;Password=eip;Timeout=5";

            // Verify Postgres actually accepts connections (container may still be starting)
            const int maxAttempts = 10;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await using var conn = new Npgsql.NpgsqlConnection(connStr);
                    await conn.OpenAsync();
                    return connStr; // Connection succeeded
                }
                catch (Exception) when (attempt < maxAttempts)
                {
                    await Task.Delay(1_000); // Wait 1s between retries
                }
            }

            return null; // All retries exhausted
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Gets the Kafka bootstrap servers string from the running TestAppHost.</summary>
    /// <remarks>
    /// Waits for the Kafka broker to become responsive before returning.
    /// Returns <c>null</c> when Docker/Kafka is unavailable so tests can skip.
    /// </remarks>
    public static async Task<string?> GetKafkaBootstrapServersAsync()
    {
        var app = await GetAppAsync();
        if (app is null) return null;

        try
        {
            var endpoint = app.GetEndpoint("kafka", "kafka-tcp");
            var bootstrapServers = $"{endpoint.Host}:{endpoint.Port}";

            // Verify Kafka actually accepts connections (container may still be starting)
            const int maxAttempts = 30;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var config = new Confluent.Kafka.AdminClientConfig
                    {
                        BootstrapServers = bootstrapServers,
                        SocketTimeoutMs = 3_000,
                    };
                    using var admin = new Confluent.Kafka.AdminClientBuilder(config).Build();
                    admin.GetMetadata(TimeSpan.FromSeconds(3));
                    return bootstrapServers; // Connection succeeded
                }
                catch (Exception) when (attempt < maxAttempts)
                {
                    await Task.Delay(2_000); // Wait 2s between retries
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Gets the Pulsar service URL from the running TestAppHost.</summary>
    /// <remarks>
    /// Waits for the Pulsar broker to become responsive before returning.
    /// Returns <c>null</c> when Docker/Pulsar is unavailable so tests can skip.
    /// </remarks>
    public static async Task<string?> GetPulsarServiceUrlAsync()
    {
        var app = await GetAppAsync();
        if (app is null) return null;

        try
        {
            var endpoint = app.GetEndpoint("pulsar", "pulsar-tcp");
            var serviceUrl = $"pulsar://{endpoint.Host}:{endpoint.Port}";

            // Verify Pulsar actually accepts connections via admin API
            var adminEndpoint = app.GetEndpoint("pulsar", "pulsar-admin");
            var adminUrl = $"http://{adminEndpoint.Host}:{adminEndpoint.Port}";

            const int maxAttempts = 60; // Pulsar standalone takes longer to start
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var response = await httpClient.GetAsync($"{adminUrl}/admin/v2/brokers/health");
                    if (response.IsSuccessStatusCode)
                        return serviceUrl;
                }
                catch (Exception) when (attempt < maxAttempts)
                {
                    // Container still starting
                }
                await Task.Delay(2_000);
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Stops the test host.</summary>
    public static async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
            _app = null;
            _attempted = false;
        }
    }
}
