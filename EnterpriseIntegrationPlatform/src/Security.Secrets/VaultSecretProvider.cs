using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// <see cref="ISecretProvider"/> implementation that communicates with a HashiCorp Vault
/// server using the KV v2 secrets engine REST API over HTTP.
/// Thread-safe via <see cref="SemaphoreSlim"/> for concurrent access.
/// </summary>
public sealed class VaultSecretProvider : ISecretProvider, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VaultSecretProvider> _logger;
    private readonly SecretAuditLogger? _auditLogger;
    private readonly SecretsOptions _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentLeaseTracker _leases = new();

    /// <summary>
    /// Initializes a new instance of <see cref="VaultSecretProvider"/>.
    /// </summary>
    /// <param name="httpClient">HTTP client configured for the Vault server.</param>
    /// <param name="options">Secrets configuration options.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="auditLogger">Optional audit logger for recording access events.</param>
    public VaultSecretProvider(
        HttpClient httpClient,
        IOptions<SecretsOptions> options,
        ILogger<VaultSecretProvider> logger,
        SecretAuditLogger? auditLogger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _auditLogger = auditLogger;

        if (!string.IsNullOrWhiteSpace(_options.VaultToken))
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Vault-Token", _options.VaultToken);
        }
    }

    /// <inheritdoc />
    public async Task<SecretEntry?> GetSecretAsync(string key, string? version = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await _semaphore.WaitAsync(ct);
        try
        {
            var path = BuildReadPath(key, version);
            using var response = await _httpClient.GetAsync(path, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Vault GET {Path} returned {StatusCode}", path, response.StatusCode);
                _auditLogger?.LogRead(key, version, success: false);
                return null;
            }

            var vaultResponse = await response.Content.ReadFromJsonAsync<VaultKvV2ReadResponse>(cancellationToken: ct);
            if (vaultResponse?.Data?.Data is null)
            {
                _auditLogger?.LogRead(key, version, success: false);
                return null;
            }

            var secretVersion = vaultResponse.Data.Metadata?.Version?.ToString() ?? "0";
            var createdAt = vaultResponse.Data.Metadata?.CreatedTime ?? DateTimeOffset.UtcNow;

            vaultResponse.Data.Data.TryGetValue("value", out var secretValue);

            if (vaultResponse.LeaseId is not null)
            {
                _leases.Track(key, vaultResponse.LeaseId, vaultResponse.LeaseDuration);
            }

            var entry = new SecretEntry(
                key,
                secretValue ?? string.Empty,
                secretVersion,
                createdAt,
                Metadata: vaultResponse.Data.Data);

            _auditLogger?.LogRead(key, secretVersion, success: true);
            return entry;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SecretEntry> SetSecretAsync(string key, string value, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        await _semaphore.WaitAsync(ct);
        try
        {
            var path = BuildWritePath(key);
            var data = new Dictionary<string, string> { ["value"] = value };
            if (metadata is not null)
            {
                foreach (var kvp in metadata)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }

            var payload = new { data };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync(path, content, ct);
            response.EnsureSuccessStatusCode();

            var vaultResponse = await response.Content.ReadFromJsonAsync<VaultKvV2WriteResponse>(cancellationToken: ct);
            var secretVersion = vaultResponse?.Data?.Version?.ToString() ?? "1";
            var createdAt = vaultResponse?.Data?.CreatedTime ?? DateTimeOffset.UtcNow;

            var entry = new SecretEntry(key, value, secretVersion, createdAt, Metadata: metadata);
            _auditLogger?.LogWrite(key, secretVersion);
            return entry;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSecretAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await _semaphore.WaitAsync(ct);
        try
        {
            var path = $"/v1/{_options.VaultMountPath}/metadata/{key}";
            using var response = await _httpClient.DeleteAsync(path, ct);

            var success = response.IsSuccessStatusCode;
            _auditLogger?.LogDelete(key, success: success);

            if (!success)
            {
                _logger.LogWarning("Vault DELETE {Path} returned {StatusCode}", path, response.StatusCode);
            }

            _leases.Remove(key);
            return success;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListSecretKeysAsync(string? prefix = null, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var listPath = $"/v1/{_options.VaultMountPath}/metadata/{prefix ?? string.Empty}";
            var request = new HttpRequestMessage(HttpMethod.Get, listPath + "?list=true");
            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Vault LIST {Path} returned {StatusCode}", listPath, response.StatusCode);
                return [];
            }

            var vaultResponse = await response.Content.ReadFromJsonAsync<VaultListResponse>(cancellationToken: ct);
            return vaultResponse?.Data?.Keys?.AsReadOnly() ?? (IReadOnlyList<string>)[];
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Releases the semaphore and clears lease tracking data.
    /// </summary>
    public void Dispose()
    {
        _semaphore.Dispose();
    }

    private string BuildReadPath(string key, string? version)
    {
        var path = $"/v1/{_options.VaultMountPath}/data/{key}";
        if (version is not null)
        {
            path += $"?version={Uri.EscapeDataString(version)}";
        }

        return path;
    }

    private string BuildWritePath(string key) =>
        $"/v1/{_options.VaultMountPath}/data/{key}";

    // Vault KV v2 response DTOs for JSON deserialization
    private sealed class VaultKvV2ReadResponse
    {
        [JsonPropertyName("data")]
        public VaultKvV2DataWrapper? Data { get; set; }

        [JsonPropertyName("lease_id")]
        public string? LeaseId { get; set; }

        [JsonPropertyName("lease_duration")]
        public int LeaseDuration { get; set; }
    }

    private sealed class VaultKvV2DataWrapper
    {
        [JsonPropertyName("data")]
        public Dictionary<string, string>? Data { get; set; }

        [JsonPropertyName("metadata")]
        public VaultKvV2Metadata? Metadata { get; set; }
    }

    private sealed class VaultKvV2Metadata
    {
        [JsonPropertyName("version")]
        public int? Version { get; set; }

        [JsonPropertyName("created_time")]
        public DateTimeOffset? CreatedTime { get; set; }
    }

    private sealed class VaultKvV2WriteResponse
    {
        [JsonPropertyName("data")]
        public VaultKvV2WriteData? Data { get; set; }
    }

    private sealed class VaultKvV2WriteData
    {
        [JsonPropertyName("version")]
        public int? Version { get; set; }

        [JsonPropertyName("created_time")]
        public DateTimeOffset? CreatedTime { get; set; }
    }

    private sealed class VaultListResponse
    {
        [JsonPropertyName("data")]
        public VaultListData? Data { get; set; }
    }

    private sealed class VaultListData
    {
        [JsonPropertyName("keys")]
        public List<string>? Keys { get; set; }
    }

    /// <summary>
    /// Tracks Vault lease IDs and durations for lease renewal.
    /// </summary>
    private sealed class ConcurrentLeaseTracker
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, LeaseInfo> _leases = new();

        public void Track(string key, string leaseId, int durationSeconds)
        {
            _leases[key] = new LeaseInfo(leaseId, durationSeconds, DateTimeOffset.UtcNow);
        }

        public void Remove(string key)
        {
            _leases.TryRemove(key, out _);
        }

        private sealed record LeaseInfo(string LeaseId, int DurationSeconds, DateTimeOffset AcquiredAt);
    }
}
