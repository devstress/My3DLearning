using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// Extension methods for registering the RuleEngine in the DI container.
/// </summary>
public static class RuleEngineServiceExtensions
{
    /// <summary>
    /// Registers the <see cref="IRuleEngine"/>, <see cref="IRuleStore"/>, and
    /// <see cref="RuleEngineOptions"/> in the DI container, binding configuration
    /// from the <c>RuleEngine</c> section.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <see cref="InMemoryRuleStore"/> as the default <see cref="IRuleStore"/>
    /// implementation. Call <see cref="AddRuleStore{TStore}"/> after this method
    /// to replace it with a persistent implementation.
    /// </para>
    /// <para>
    /// Rules defined in <see cref="RuleEngineOptions.Rules"/> are seeded into the
    /// store at registration time.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRuleEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RuleEngineOptions>(
            configuration.GetSection(RuleEngineOptions.SectionName));

        // Register the in-memory store as default (if not already registered).
        services.AddSingleton<IRuleStore>(sp =>
        {
            var store = new InMemoryRuleStore();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RuleEngineOptions>>().Value;

            // Seed pre-configured rules from options.
            foreach (var rule in options.Rules)
            {
                store.AddOrUpdateAsync(rule).GetAwaiter().GetResult();
            }

            return store;
        });

        services.AddSingleton<IRuleEngine, BusinessRuleEngine>();

        return services;
    }

    /// <summary>
    /// Replaces the default <see cref="IRuleStore"/> with a custom implementation.
    /// </summary>
    /// <typeparam name="TStore">The rule store implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRuleStore<TStore>(this IServiceCollection services)
        where TStore : class, IRuleStore
    {
        ArgumentNullException.ThrowIfNull(services);

        // Remove any previously registered IRuleStore.
        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IRuleStore));
        if (existing is not null)
            services.Remove(existing);

        services.AddSingleton<IRuleStore, TStore>();
        return services;
    }
}
