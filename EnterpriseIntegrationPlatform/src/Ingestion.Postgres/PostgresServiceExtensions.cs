using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Ingestion.Postgres;

/// <summary>
/// Extension methods for registering the PostgreSQL message broker provider.
/// Follows the same pattern as <c>AddNatsJetStreamBroker</c>, <c>AddKafkaBroker</c>,
/// and <c>AddPulsarBroker</c>.
/// </summary>
public static class PostgresServiceExtensions
{
    /// <summary>
    /// Registers the PostgreSQL message broker producer, consumer, and transactional client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">
    /// Npgsql connection string, e.g.
    /// <c>Host=localhost;Port=5432;Database=eip;Username=eip;Password=eip</c>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgresBroker(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.Configure<PostgresBrokerOptions>(o => o.ConnectionString = connectionString);

        services.AddSingleton(sp =>
            new PostgresConnectionFactory(connectionString));

        services.AddSingleton<IMessageBrokerProducer, PostgresBrokerProducer>();
        services.AddSingleton<IMessageBrokerConsumer, PostgresBrokerConsumer>();
        services.AddSingleton<ITransactionalClient, PostgresTransactionalClient>();

        return services;
    }
}
