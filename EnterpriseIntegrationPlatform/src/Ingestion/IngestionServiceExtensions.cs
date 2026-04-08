using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Extension methods for registering the configurable message broker layer.
/// The broker is selected at deployment time via the <c>Broker</c> configuration section.
/// </summary>
public static class IngestionServiceExtensions
{
    // Known broker extension methods — avoids fragile reflection heuristics.
    private static readonly IReadOnlyDictionary<BrokerType, (string AssemblyName, string TypeName, string MethodName)>
        BrokerRegistrations = new Dictionary<BrokerType, (string, string, string)>
        {
            [BrokerType.NatsJetStream] = (
                "Ingestion.Nats",
                "EnterpriseIntegrationPlatform.Ingestion.Nats.NatsServiceExtensions",
                "AddNatsJetStreamBroker"),
            [BrokerType.Kafka] = (
                "Ingestion.Kafka",
                "EnterpriseIntegrationPlatform.Ingestion.Kafka.KafkaServiceExtensions",
                "AddKafkaBroker"),
            [BrokerType.Pulsar] = (
                "Ingestion.Pulsar",
                "EnterpriseIntegrationPlatform.Ingestion.Pulsar.PulsarServiceExtensions",
                "AddPulsarBroker"),
            [BrokerType.Postgres] = (
                "Ingestion.Postgres",
                "EnterpriseIntegrationPlatform.Ingestion.Postgres.PostgresServiceExtensions",
                "AddPostgresBroker"),
        };

    /// <summary>
    /// Registers <see cref="BrokerOptions"/> from the <c>Broker</c> configuration section.
    /// Downstream provider registration methods (e.g. <c>AddNatsJetStreamBroker</c>,
    /// <c>AddKafkaBroker</c>, <c>AddPulsarBroker</c>) use these options to wire the
    /// correct <see cref="IMessageBrokerProducer"/> and <see cref="IMessageBrokerConsumer"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBrokerOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BrokerOptions>(
            configuration.GetSection(BrokerOptions.SectionName));

        return services;
    }

    /// <summary>
    /// Unified ingestion registration method. Configures <see cref="BrokerOptions"/> and
    /// automatically registers the correct <see cref="IMessageBrokerProducer"/> and
    /// <see cref="IMessageBrokerConsumer"/> based on <see cref="BrokerOptions.BrokerType"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The consuming project must reference the appropriate broker package
    /// (<c>Ingestion.Nats</c>, <c>Ingestion.Kafka</c>, <c>Ingestion.Pulsar</c>,
    /// or <c>Ingestion.Postgres</c>)
    /// for the selected <see cref="BrokerType"/>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// An action that configures <see cref="BrokerOptions"/> including
    /// <see cref="BrokerOptions.BrokerType"/> and <see cref="BrokerOptions.ConnectionString"/>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the broker assembly for the configured <see cref="BrokerType"/> cannot be loaded.
    /// </exception>
    public static IServiceCollection AddIngestion(
        this IServiceCollection services,
        Action<BrokerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new BrokerOptions();
        configure(options);
        services.Configure(configure);

        if (!BrokerRegistrations.TryGetValue(options.BrokerType, out var reg))
        {
            throw new InvalidOperationException(
                $"Unknown broker type: {options.BrokerType}.");
        }

        Assembly assembly;
        try
        {
            assembly = Assembly.Load(reg.AssemblyName);
        }
        catch (FileNotFoundException)
        {
            throw new InvalidOperationException(
                $"Broker assembly '{reg.AssemblyName}' not found. " +
                $"Add a project or package reference to use BrokerType.{options.BrokerType}.");
        }

        var extensionType = assembly.GetType(reg.TypeName)
            ?? throw new InvalidOperationException(
                $"Extension type '{reg.TypeName}' not found in assembly '{reg.AssemblyName}'.");

        var method = extensionType.GetMethod(
            reg.MethodName,
            BindingFlags.Static | BindingFlags.Public,
            [typeof(IServiceCollection), typeof(string)])
            ?? throw new InvalidOperationException(
                $"Method '{reg.MethodName}(IServiceCollection, string)' not found on '{reg.TypeName}'.");

        method.Invoke(null, [services, options.ConnectionString]);

        return services;
    }
}
