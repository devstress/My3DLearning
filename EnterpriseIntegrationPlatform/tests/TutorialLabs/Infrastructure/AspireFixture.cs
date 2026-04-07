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

    [OneTimeSetUp]
    public async Task GlobalSetUp()
    {
        var app = await SharedTestAppHost.GetAppAsync();
        IsAvailable = app is not null;

        if (IsAvailable)
        {
            NatsUrl = await SharedTestAppHost.GetNatsUrlAsync();
            TemporalAddress = await SharedTestAppHost.GetTemporalAddressAsync();
            SftpEndpoint = await SharedTestAppHost.GetSftpEndpointAsync();
            SmtpEndpoint = await SharedTestAppHost.GetSmtpEndpointAsync();
        }
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        await SharedTestAppHost.DisposeAsync();
    }

    /// <summary>
    /// Creates a NatsBrokerEndpoint connected to the real NATS JetStream.
    /// Throws Ignore if Docker is not available.
    /// </summary>
    public static NatsBrokerEndpoint CreateNatsEndpoint(string name)
    {
        if (!IsAvailable || NatsUrl is null)
            Assert.Ignore("Docker not available — skipping real broker test");
        return new NatsBrokerEndpoint(name, NatsUrl);
    }

    /// <summary>
    /// Generates a unique topic name to prevent cross-test interference.
    /// </summary>
    public static string UniqueTopic(string prefix = "test") =>
        $"{prefix}-{Guid.NewGuid():N}";
}
