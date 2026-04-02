using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class CassandraServiceExtensionsTests
{
    [Test]
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
        Assert.That(descriptors, Has.Count.EqualTo(1));
        Assert.That(descriptors[0].Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
    }

    [Test]
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
        Assert.That(descriptors, Has.Count.EqualTo(1));
        Assert.That(descriptors[0].Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
    }

    [Test]
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
        Assert.That(options.Value.ContactPoints, Is.EqualTo("db1.example.com,db2.example.com"));
        Assert.That(options.Value.Port, Is.EqualTo(19042));
        Assert.That(options.Value.Keyspace, Is.EqualTo("production_eip"));
        Assert.That(options.Value.ReplicationFactor, Is.EqualTo(5));
        Assert.That(options.Value.DefaultTtlSeconds, Is.EqualTo(86400));
    }

    [Test]
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

        Assert.That(healthCheckOptions.Value.Registrations.Any(r => r.Name == "cassandra"), Is.True);
    }
}
