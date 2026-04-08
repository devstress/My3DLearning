using Confluent.Kafka;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        services.Configure<KafkaOptions>(opts => opts.BootstrapServers = bootstrapServers);

        services.AddSingleton<IProducer<string, byte[]>>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
            opts.Validate();

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = opts.BootstrapServers,
                Acks = ParseAcks(opts.Acks),
                EnableIdempotence = opts.EnableIdempotence,
                CompressionType = ParseCompressionType(opts.CompressionType),
                LingerMs = opts.LingerMs,
                BatchSize = opts.BatchSize,
            };

            return new ProducerBuilder<string, byte[]>(producerConfig).Build();
        });

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
            return new ConsumerConfig
            {
                BootstrapServers = opts.BootstrapServers,
                SessionTimeoutMs = opts.SessionTimeoutMs,
                GroupId = opts.GroupId,
                AutoOffsetReset = ParseAutoOffsetReset(opts.AutoOffsetReset),
                EnableAutoCommit = opts.EnableAutoCommit,
            };
        });

        services.AddSingleton<IMessageBrokerProducer, KafkaProducer>();
        services.AddSingleton<IMessageBrokerConsumer, KafkaConsumer>();
        services.AddSingleton<KafkaHealthCheck>();

        return services;
    }

    private static Confluent.Kafka.Acks ParseAcks(string value) => value.ToLowerInvariant() switch
    {
        "all" or "-1" => Confluent.Kafka.Acks.All,
        "none" or "0" => Confluent.Kafka.Acks.None,
        "leader" or "1" => Confluent.Kafka.Acks.Leader,
        _ => Confluent.Kafka.Acks.All,
    };

    private static Confluent.Kafka.CompressionType ParseCompressionType(string value) => value.ToLowerInvariant() switch
    {
        "gzip" => Confluent.Kafka.CompressionType.Gzip,
        "snappy" => Confluent.Kafka.CompressionType.Snappy,
        "lz4" => Confluent.Kafka.CompressionType.Lz4,
        "zstd" => Confluent.Kafka.CompressionType.Zstd,
        _ => Confluent.Kafka.CompressionType.None,
    };

    private static Confluent.Kafka.AutoOffsetReset ParseAutoOffsetReset(string value) => value.ToLowerInvariant() switch
    {
        "latest" => Confluent.Kafka.AutoOffsetReset.Latest,
        "error" => Confluent.Kafka.AutoOffsetReset.Error,
        _ => Confluent.Kafka.AutoOffsetReset.Earliest,
    };
}
