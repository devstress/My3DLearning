using Confluent.Kafka;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Ingestion.Kafka;

/// <summary>
/// Extension methods for registering the Apache Kafka message broker provider.
/// </summary>
public static class KafkaServiceExtensions
{
    /// <summary>
    /// Registers the Apache Kafka message broker producer and consumer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="bootstrapServers">
    /// Kafka bootstrap servers, e.g. <c>localhost:9092</c>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafkaBroker(
        this IServiceCollection services,
        string bootstrapServers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapServers);

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
        };

        services.AddSingleton<IProducer<string, byte[]>>(_ =>
            new ProducerBuilder<string, byte[]>(producerConfig).Build());

        services.AddSingleton(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
        });

        services.AddSingleton<IMessageBrokerProducer, KafkaProducer>();
        services.AddSingleton<IMessageBrokerConsumer, KafkaConsumer>();

        return services;
    }
}
