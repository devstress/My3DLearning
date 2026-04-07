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
    public static async Task<string?> GetPostgresConnectionStringAsync()
    {
        var app = await GetAppAsync();
        if (app is null) return null;

        var endpoint = app.GetEndpoint("postgres", "postgres-tcp");
        return $"Host={endpoint.Host};Port={endpoint.Port};Database=eip;Username=eip;Password=eip";
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
