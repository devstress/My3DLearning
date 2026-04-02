using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// <see cref="ISecretProvider"/> implementation that communicates with Azure Key Vault
/// using the Key Vault REST API over HTTP with Azure AD authentication.
/// Thread-safe via <see cref="SemaphoreSlim"/> for concurrent access.
/// </summary>
public sealed class AzureKeyVaultSecretProvider : ISecretProvider, IDisposable
{
    private const string ApiVersion = "7.4";
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureKeyVaultSecretProvider> _logger;
    private readonly SecretAuditLogger? _auditLogger;
    private readonly SecretsOptions _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of <see cref="AzureKeyVaultSecretProvider"/>.
    /// </summary>
    /// <param name="httpClient">HTTP client for Key Vault and Azure AD requests.</param>
    /// <param name="options">Secrets configuration options.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="auditLogger">Optional audit logger for recording access events.</param>
    public AzureKeyVaultSecretProvider(
        HttpClient httpClient,
        IOptions<SecretsOptions> options,
        ILogger<AzureKeyVaultSecretProvider> logger,
        SecretAuditLogger? auditLogger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _auditLogger = auditLogger;
    }

    /// <inheritdoc />
    public async Task<SecretEntry?> GetSecretAsync(string key, string? version = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await _semaphore.WaitAsync(ct);
        try
        {
            await EnsureAuthenticatedAsync(ct);

            var path = version is not null
                ? $"{_options.AzureKeyVaultUri}/secrets/{key}/{Uri.EscapeDataString(version)}?api-version={ApiVersion}"
                : $"{_options.AzureKeyVaultUri}/secrets/{key}?api-version={ApiVersion}";

            using var response = await _httpClient.GetAsync(path, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure Key Vault GET {Path} returned {StatusCode}", path, response.StatusCode);
                _auditLogger?.LogRead(key, version, success: false);
                return null;
            }

            var kvResponse = await response.Content.ReadFromJsonAsync<AzureSecretResponse>(cancellationToken: ct);
            if (kvResponse is null)
            {
                _auditLogger?.LogRead(key, version, success: false);
                return null;
            }

            var secretVersion = ExtractVersionFromId(kvResponse.Id);
            var metadata = kvResponse.Tags is not null
                ? new Dictionary<string, string>(kvResponse.Tags)
                : null;

            DateTimeOffset? expiresAt = kvResponse.Attributes?.Expires.HasValue == true
                ? DateTimeOffset.FromUnixTimeSeconds(kvResponse.Attributes.Expires.Value)
                : null;

            var createdAt = kvResponse.Attributes?.Created.HasValue == true
                ? DateTimeOffset.FromUnixTimeSeconds(kvResponse.Attributes.Created.Value)
                : DateTimeOffset.UtcNow;

            var entry = new SecretEntry(
                key,
                kvResponse.Value ?? string.Empty,
                secretVersion,
                createdAt,
                expiresAt,
                metadata);

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
            await EnsureAuthenticatedAsync(ct);

            var path = $"{_options.AzureKeyVaultUri}/secrets/{key}?api-version={ApiVersion}";
            var payload = new AzureSetSecretRequest
            {
                Value = value,
                Tags = metadata is not null ? new Dictionary<string, string>(metadata) : null
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PutAsync(path, content, ct);
            response.EnsureSuccessStatusCode();

            var kvResponse = await response.Content.ReadFromJsonAsync<AzureSecretResponse>(cancellationToken: ct);
            var secretVersion = kvResponse?.Id is not null ? ExtractVersionFromId(kvResponse.Id) : "1";

            var entry = new SecretEntry(key, value, secretVersion, DateTimeOffset.UtcNow, Metadata: metadata);
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
            await EnsureAuthenticatedAsync(ct);

            var path = $"{_options.AzureKeyVaultUri}/secrets/{key}?api-version={ApiVersion}";
            using var response = await _httpClient.DeleteAsync(path, ct);

            var success = response.IsSuccessStatusCode;
            _auditLogger?.LogDelete(key, success: success);

            if (!success)
            {
                _logger.LogWarning("Azure Key Vault DELETE {Path} returned {StatusCode}", path, response.StatusCode);
            }

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
            await EnsureAuthenticatedAsync(ct);

            var path = $"{_options.AzureKeyVaultUri}/secrets?api-version={ApiVersion}";
            using var response = await _httpClient.GetAsync(path, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure Key Vault LIST returned {StatusCode}", response.StatusCode);
                return [];
            }

            var listResponse = await response.Content.ReadFromJsonAsync<AzureSecretListResponse>(cancellationToken: ct);
            var keys = listResponse?.Value?
                .Select(s => ExtractNameFromId(s.Id))
                .Where(name => prefix is null || name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? [];

            return keys.AsReadOnly();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Releases the semaphore.
    /// </summary>
    public void Dispose()
    {
        _semaphore.Dispose();
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (_accessToken is not null && _tokenExpiry > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.AzureTenantId) ||
            string.IsNullOrWhiteSpace(_options.AzureClientId) ||
            string.IsNullOrWhiteSpace(_options.AzureClientSecret))
        {
            return;
        }

        var tokenUrl = $"https://login.microsoftonline.com/{_options.AzureTenantId}/oauth2/v2.0/token";
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.AzureClientId,
            ["client_secret"] = _options.AzureClientSecret,
            ["scope"] = "https://vault.azure.net/.default"
        };

        using var tokenResponse = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(formData), ct);
        if (tokenResponse.IsSuccessStatusCode)
        {
            var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<AzureTokenResponse>(cancellationToken: ct);
            if (tokenResult?.AccessToken is not null)
            {
                _accessToken = tokenResult.AccessToken;
                _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(tokenResult.ExpiresIn > 0 ? tokenResult.ExpiresIn : 3600);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
        }
        else
        {
            _logger.LogWarning("Azure AD token acquisition failed with {StatusCode}", tokenResponse.StatusCode);
        }
    }

    private static string ExtractVersionFromId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return "1";
        }

        var parts = id.Split('/');
        return parts.Length > 0 ? parts[^1] : "1";
    }

    private static string ExtractNameFromId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return string.Empty;
        }

        var uri = new Uri(id);
        var segments = uri.Segments;
        return segments.Length >= 3
            ? segments[2].TrimEnd('/')
            : string.Empty;
    }

    // Azure Key Vault REST API DTOs
    private sealed class AzureSecretResponse
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("attributes")]
        public AzureSecretAttributes? Attributes { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }

    private sealed class AzureSecretAttributes
    {
        [JsonPropertyName("created")]
        public long? Created { get; set; }

        [JsonPropertyName("exp")]
        public long? Expires { get; set; }

        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }
    }

    private sealed class AzureSetSecretRequest
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }

    private sealed class AzureSecretListResponse
    {
        [JsonPropertyName("value")]
        public List<AzureSecretListItem>? Value { get; set; }
    }

    private sealed class AzureSecretListItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private sealed class AzureTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
