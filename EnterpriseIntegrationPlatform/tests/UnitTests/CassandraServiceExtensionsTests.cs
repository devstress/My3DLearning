using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class CassandraServiceExtensionsTests
{
    [Fact]
    public void AddCassandraStorage_RegistersSessionFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cassandra:ContactPoints"] = "node1,node2",
                ["Cassandra:Port"] = "9042",
                ["Cassandra:Keyspace"] = "test_ks",
            })
            .Build();

        services.AddLogging();

        // Act
        services.AddCassandraStorage(configuration);

        // Assert
        var descriptors = services.Where(d => d.ServiceType == typeof(ICassandraSessionFactory)).ToList();
        descriptors.Should().ContainSingle();
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddCassandraStorage_RegistersMessageRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging();

        // Act
        services.AddCassandraStorage(configuration);

        // Assert
        var descriptors = services.Where(d => d.ServiceType == typeof(IMessageRepository)).ToList();
        descriptors.Should().ContainSingle();
        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddCassandraStorage_BindsOptionsFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cassandra:ContactPoints"] = "db1.example.com,db2.example.com",
                ["Cassandra:Port"] = "19042",
                ["Cassandra:Keyspace"] = "production_eip",
                ["Cassandra:ReplicationFactor"] = "5",
                ["Cassandra:DefaultTtlSeconds"] = "86400",
            })
            .Build();

        services.AddLogging();
        services.AddCassandraStorage(configuration);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CassandraOptions>>();

        // Assert
        options.Value.ContactPoints.Should().Be("db1.example.com,db2.example.com");
        options.Value.Port.Should().Be(19042);
        options.Value.Keyspace.Should().Be("production_eip");
        options.Value.ReplicationFactor.Should().Be(5);
        options.Value.DefaultTtlSeconds.Should().Be(86400);
    }

    [Fact]
    public void AddCassandraStorage_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging();

        // Act
        services.AddCassandraStorage(configuration);

        // Assert
        var healthCheckDescriptors = services
            .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck)
                        || d.ImplementationType == typeof(CassandraHealthCheck))
            .ToList();

        // Health check registration goes through AddHealthChecks().AddCheck<T>(),
        // which registers via IHealthCheckRegistration. Verify the check is present
        // by building the provider and resolving IHealthCheckService.
        var provider = services.BuildServiceProvider();
        var healthCheckOptions = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>>();

        healthCheckOptions.Value.Registrations
            .Should().Contain(r => r.Name == "cassandra");
    }
}
