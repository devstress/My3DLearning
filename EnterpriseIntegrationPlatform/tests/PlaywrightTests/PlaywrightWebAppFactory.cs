using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace EnterpriseIntegrationPlatform.Tests.Playwright;

/// <summary>
/// Starts a real ASP.NET application as a child process with Kestrel on a
/// random port.  This is required for Playwright E2E tests because the
/// external Chromium browser process cannot reach the in-memory
/// <c>TestServer</c> used by <c>WebApplicationFactory</c>.
/// <para>
/// The factory locates the entry-point assembly for <typeparamref name="TEntryPoint"/>,
/// runs it via <c>dotnet exec</c> with <c>--urls</c> set to a random port,
/// and waits until the server is accepting connections before returning.
/// </para>
/// </summary>
internal sealed class PlaywrightWebAppFactory<TEntryPoint> : IDisposable
    where TEntryPoint : class
{
    private readonly Process _process;

    /// <summary>
    /// The base URL of the real Kestrel server (e.g. <c>http://127.0.0.1:54321</c>).
    /// </summary>
    public string ServerAddress { get; }

    /// <summary>
    /// A plain <see cref="System.Net.Http.HttpClient"/> pointed at the real
    /// server address.  Use for API-level assertions alongside Playwright.
    /// </summary>
    public HttpClient HttpClient { get; }

    public PlaywrightWebAppFactory()
    {
        var port = GetAvailablePort();
        ServerAddress = $"http://127.0.0.1:{port}";

        var assembly = typeof(TEntryPoint).Assembly;
        var dllPath = assembly.Location;

        // Resolve the source project directory by searching upward from the DLL
        // for the repository root (identified by a .sln file), then descending
        // into src/<ProjectName>/.  This is more robust than counting directory
        // levels, which would break if the build output structure ever changes.
        var projectName = Path.GetFileNameWithoutExtension(dllPath);
        var contentRoot = FindProjectRoot(dllPath, projectName)
            ?? Path.GetDirectoryName(dllPath)!;

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList = { "exec", dllPath, "--urls", ServerAddress },
                WorkingDirectory = contentRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Environment =
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["DOTNET_NOLOGO"] = "1",
                },
            },
        };

        _process.Start();

        // Drain stdout/stderr asynchronously to prevent blocking.
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // Wait until the server is accepting connections.
        WaitForServer(port, timeout: TimeSpan.FromSeconds(15));

        HttpClient = new HttpClient { BaseAddress = new Uri(ServerAddress) };
    }

    public void Dispose()
    {
        HttpClient.Dispose();

        if (!_process.HasExited)
        {
            try { _process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            _process.WaitForExit(5000);
        }

        _process.Dispose();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// Searches upward from <paramref name="startPath"/> for a directory
    /// containing a <c>.sln</c> file (the repo root), then returns
    /// <c>{repoRoot}/src/{projectName}</c> if it exists.
    /// </summary>
    private static string? FindProjectRoot(string startPath, string projectName)
    {
        var dir = Path.GetDirectoryName(startPath);
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0)
            {
                var candidate = Path.Combine(dir, "src", projectName);
                return Directory.Exists(candidate) ? candidate : null;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    private void WaitForServer(int port, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Server process exited with code {_process.ExitCode} " +
                    "before accepting connections.");
            }

            try
            {
                using var tcp = new TcpClient();
                tcp.Connect(IPAddress.Loopback, port);
                return; // Server is accepting connections.
            }
            catch (SocketException)
            {
                Thread.Sleep(100);
            }
        }

        throw new TimeoutException(
            $"Server did not start accepting connections on port {port} " +
            $"within {timeout.TotalSeconds}s.");
    }
}

