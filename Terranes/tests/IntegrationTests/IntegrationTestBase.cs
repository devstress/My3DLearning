using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Terranes.IntegrationTests;

/// <summary>
/// Base class for all integration tests. Provides a shared <see cref="HttpClient"/>
/// backed by WebApplicationFactory&lt;Program&gt; so every test exercises the full
/// API pipeline (routing → endpoint → service → response).
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    protected HttpClient Client { get; }

    protected IntegrationTestBase()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot(Path.Combine(FindSolutionRoot(), "src", "Platform.Api"));
                builder.UseEnvironment("Development");
            });
        Client = _factory.CreateClient();
    }

    /// <summary>
    /// Walks up from the executing assembly directory to find the directory
    /// containing the Terranes.slnx file.
    /// </summary>
    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "Terranes.slnx")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not find Terranes.slnx in any parent directory.");
    }

    /// <summary>Posts a JSON payload and returns the deserialized response.</summary>
    protected async Task<T> PostAsync<T>(string url, object payload)
    {
        var response = await Client.PostAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>();
        return result!;
    }

    /// <summary>Gets JSON from a URL and deserializes it.</summary>
    protected async Task<T> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>();
        return result!;
    }

    /// <summary>Puts a JSON payload and returns the deserialized response.</summary>
    protected async Task<T> PutAsync<T>(string url, object? payload = null)
    {
        var response = await Client.PutAsJsonAsync(url, payload ?? new { });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>();
        return result!;
    }

    public void Dispose()
    {
        Client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
