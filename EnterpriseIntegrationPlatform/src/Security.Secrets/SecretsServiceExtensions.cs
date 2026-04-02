using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Extension methods for registering secrets management services with the DI container.
/// </summary>
public static class SecretsServiceExtensions
{
    /// <summary>
    /// Registers in-memory secrets management services suitable for development and testing.
    /// Configures <see cref="InMemorySecretProvider"/> as the <see cref="ISecretProvider"/>,
    /// along with caching, rotation, and audit logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSecretsManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddCoreServices(services, configuration);
        services.TryAddSingleton<InMemorySecretProvider>();
        services.TryAddSingleton<ISecretProvider>(sp =>
        {
            var inner = sp.GetRequiredService<InMemorySecretProvider>();
            var options = sp.GetRequiredService<IOptions<SecretsOptions>>().Value;
            var auditLogger = sp.GetService<SecretAuditLogger>();
            return new CachedSecretProvider(inner, options.CacheTtl, auditLogger);
        });
        AddRotationServices(services);

        return services;
    }

    /// <summary>
    /// Registers HashiCorp Vault-backed secrets management services.
    /// Configures <see cref="VaultSecretProvider"/> as the <see cref="ISecretProvider"/>,
    /// along with caching, rotation, and audit logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultSecretsManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddCoreServices(services, configuration);
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SecretsOptions>>().Value;
            var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(options.VaultAddress))
            {
                client.BaseAddress = new Uri(options.VaultAddress);
            }

            return new VaultSecretProvider(
                client,
                sp.GetRequiredService<IOptions<SecretsOptions>>(),
                sp.GetRequiredService<ILogger<VaultSecretProvider>>(),
                sp.GetService<SecretAuditLogger>());
        });
        services.TryAddSingleton<ISecretProvider>(sp =>
        {
            var vault = sp.GetRequiredService<VaultSecretProvider>();
            var options = sp.GetRequiredService<IOptions<SecretsOptions>>().Value;
            var auditLogger = sp.GetService<SecretAuditLogger>();
            return new CachedSecretProvider(vault, options.CacheTtl, auditLogger);
        });
        AddRotationServices(services);

        return services;
    }

    /// <summary>
    /// Registers Azure Key Vault-backed secrets management services.
    /// Configures <see cref="AzureKeyVaultSecretProvider"/> as the <see cref="ISecretProvider"/>,
    /// along with caching, rotation, and audit logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureKeyVaultSecretsManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddCoreServices(services, configuration);
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SecretsOptions>>().Value;
            var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(options.AzureKeyVaultUri))
            {
                client.BaseAddress = new Uri(options.AzureKeyVaultUri);
            }

            return new AzureKeyVaultSecretProvider(
                client,
                sp.GetRequiredService<IOptions<SecretsOptions>>(),
                sp.GetRequiredService<ILogger<AzureKeyVaultSecretProvider>>(),
                sp.GetService<SecretAuditLogger>());
        });
        services.TryAddSingleton<ISecretProvider>(sp =>
        {
            var azure = sp.GetRequiredService<AzureKeyVaultSecretProvider>();
            var options = sp.GetRequiredService<IOptions<SecretsOptions>>().Value;
            var auditLogger = sp.GetService<SecretAuditLogger>();
            return new CachedSecretProvider(azure, options.CacheTtl, auditLogger);
        });
        AddRotationServices(services);

        return services;
    }

    private static void AddCoreServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SecretsOptions>(configuration.GetSection(SecretsOptions.SectionName));
        services.TryAddSingleton<SecretAuditLogger>();
    }

    private static void AddRotationServices(IServiceCollection services)
    {
        services.TryAddSingleton<SecretRotationService>();
        services.TryAddSingleton<ISecretRotationService>(sp => sp.GetRequiredService<SecretRotationService>());
        services.AddHostedService(sp => sp.GetRequiredService<SecretRotationService>());
    }
}
