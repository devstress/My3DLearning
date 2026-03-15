using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace EnterpriseIntegrationPlatform.Storage.Cassandra;

/// <summary>
/// Extension methods for registering the Cassandra storage services
/// with the dependency injection container.
/// </summary>
public static class CassandraServiceExtensions
{
    /// <summary>
    /// Registers the Cassandra storage module: session factory, message repository,
    /// health check, and OpenTelemetry instrumentation. Configuration is read from
    /// the <c>Cassandra</c> section in <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCassandraStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CassandraOptions>(
            configuration.GetSection(CassandraOptions.SectionName));

        services.AddSingleton<ICassandraSessionFactory, CassandraSessionFactory>();
        services.AddSingleton<IMessageRepository, CassandraMessageRepository>();

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource(CassandraDiagnostics.SourceName);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(CassandraDiagnostics.SourceName);
            });

        services.AddHealthChecks()
            .AddCheck<CassandraHealthCheck>("cassandra", tags: ["ready"]);

        return services;
    }
}
