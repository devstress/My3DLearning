using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Integration;

/// <summary>
/// Namespace-level setup fixture that starts a single Grafana Loki container
/// shared across all integration test classes.
/// <para>
/// Starting a Testcontainer takes ~10–15 seconds. Previously each of the 17
/// integration tests started its own container, resulting in ~5 minutes of
/// container-startup overhead alone. With this shared fixture the container
/// is started once and reused, reducing total time to ~30–40 seconds.
/// </para>
/// </summary>
[SetUpFixture]
public class SharedLokiFixture
{
    private static IContainer? _lokiContainer;

    /// <summary>Base URL (e.g. <c>http://localhost:12345</c>) for the shared Loki instance.</summary>
    public static string LokiBaseUrl { get; private set; } = "";

    /// <summary>Whether Docker / Testcontainers are available on this machine.</summary>
    public static bool DockerAvailable { get; private set; }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        try
        {
            _lokiContainer = new ContainerBuilder()
                .WithImage("grafana/loki:3.4.2")
                .WithPortBinding(3100, true)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r.ForPort(3100).ForPath("/ready")))
                .Build();

            await _lokiContainer.StartAsync();
            DockerAvailable = true;

            var host = _lokiContainer.Hostname;
            var port = _lokiContainer.GetMappedPublicPort(3100);
            LokiBaseUrl = $"http://{host}:{port}";
        }
        catch (Exception)
        {
            DockerAvailable = false;
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_lokiContainer is not null)
        {
            await _lokiContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Polls Loki until the query returns at least <paramref name="expectedCount"/>
    /// results, replacing the previous fixed <c>Task.Delay(2000)</c>.
    /// Falls back to a short fixed wait if no count check is needed.
    /// </summary>
    public static async Task WaitForLokiIndexAsync(
        Func<Task<int>> countQuery,
        int expectedCount,
        int maxRetries = 30,
        int delayMs = 200)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            var count = await countQuery();
            if (count >= expectedCount) return;
            await Task.Delay(delayMs);
        }
    }
}
