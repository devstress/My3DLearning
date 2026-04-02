using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Background service that monitors secret rotation policies and triggers rotation
/// when secrets approach expiry. Implements <see cref="IHostedService"/> for lifecycle management
/// and <see cref="ISecretRotationService"/> for policy registration.
/// All rotation events are logged via <see cref="SecretAuditLogger"/> for audit compliance.
/// </summary>
public sealed class SecretRotationService : BackgroundService, ISecretRotationService
{
    private readonly ISecretProvider _provider;
    private readonly SecretAuditLogger _auditLogger;
    private readonly ILogger<SecretRotationService> _logger;
    private readonly SecretsOptions _options;
    private readonly ConcurrentDictionary<string, SecretRotationPolicy> _policies = new();

    /// <summary>
    /// Initializes a new instance of <see cref="SecretRotationService"/>.
    /// </summary>
    /// <param name="provider">The secret provider used to read and write secrets during rotation.</param>
    /// <param name="auditLogger">Audit logger for recording rotation events.</param>
    /// <param name="options">Secrets configuration options.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public SecretRotationService(
        ISecretProvider provider,
        SecretAuditLogger auditLogger,
        IOptions<SecretsOptions> options,
        ILogger<SecretRotationService> logger)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(auditLogger);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _provider = provider;
        _auditLogger = auditLogger;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task RegisterPolicyAsync(string key, SecretRotationPolicy policy, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(policy);

        _policies[key] = policy;
        _logger.LogInformation("Registered rotation policy for secret {Key} with interval {Interval}", key, policy.RotationInterval);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> UnregisterPolicyAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var removed = _policies.TryRemove(key, out _);
        if (removed)
        {
            _logger.LogInformation("Unregistered rotation policy for secret {Key}", key);
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<SecretRotationPolicy?> GetPolicyAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _policies.TryGetValue(key, out var policy);
        return Task.FromResult(policy);
    }

    /// <inheritdoc />
    public async Task<SecretEntry> RotateNowAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _logger.LogInformation("Rotating secret {Key}", key);

        var existing = await _provider.GetSecretAsync(key, ct: ct);
        var newValue = GenerateRotatedValue();

        var newEntry = await _provider.SetSecretAsync(
            key,
            newValue,
            existing?.Metadata,
            ct);

        _auditLogger.LogRotation(key, newEntry.Version, success: true, detail: "Manual rotation");
        return newEntry;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Secret rotation service started with check interval {Interval}", _options.RotationCheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.RotationCheckInterval, stoppingToken);
                await CheckAndRotateSecretsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during secret rotation check");
            }
        }

        _logger.LogInformation("Secret rotation service stopped");
    }

    private async Task CheckAndRotateSecretsAsync(CancellationToken ct)
    {
        foreach (var (key, policy) in _policies)
        {
            if (!policy.AutoRotate)
            {
                continue;
            }

            try
            {
                var secret = await _provider.GetSecretAsync(key, ct: ct);
                if (secret is null)
                {
                    _logger.LogWarning("Secret {Key} not found during rotation check", key);
                    continue;
                }

                if (ShouldRotate(secret, policy))
                {
                    _logger.LogInformation("Auto-rotating secret {Key} per policy", key);
                    var newValue = GenerateRotatedValue();
                    var newEntry = await _provider.SetSecretAsync(key, newValue, secret.Metadata, ct);
                    _auditLogger.LogRotation(key, newEntry.Version, success: true, detail: "Auto-rotation per policy");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rotate secret {Key}", key);
                _auditLogger.LogRotation(key, success: false, detail: ex.Message);
            }
        }
    }

    private static bool ShouldRotate(SecretEntry secret, SecretRotationPolicy policy)
    {
        var age = DateTimeOffset.UtcNow - secret.CreatedAt;
        if (age >= policy.RotationInterval)
        {
            return true;
        }

        if (policy.RotateBeforeExpiry.HasValue && secret.ExpiresAt.HasValue)
        {
            var timeUntilExpiry = secret.ExpiresAt.Value - DateTimeOffset.UtcNow;
            if (timeUntilExpiry <= policy.RotateBeforeExpiry.Value)
            {
                return true;
            }
        }

        return false;
    }

    private static string GenerateRotatedValue()
    {
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
