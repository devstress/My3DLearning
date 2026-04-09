using EnterpriseIntegrationPlatform.Ingestion.Postgres;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PostgresBrokerConsumerTests
{
    private static readonly NullLogger<PostgresBrokerConsumer> Logger = NullLogger<PostgresBrokerConsumer>.Instance;
    private static readonly IOptions<PostgresBrokerOptions> Opts = Options.Create(new PostgresBrokerOptions());

    [Test]
    public void Constructor_NullFactory_Throws()
    {
        Assert.That(
            () => new PostgresBrokerConsumer(null!, Opts, Logger),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("factory"));
    }

    [Test]
    public void Constructor_NullOptions_Throws()
    {
        var factory = new PostgresConnectionFactory("Host=localhost;Database=eip;Username=t;Password=t");
        try
        {
            Assert.That(
                () => new PostgresBrokerConsumer(factory, null!, Logger),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
        }
        finally { factory.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        var factory = new PostgresConnectionFactory("Host=localhost;Database=eip;Username=t;Password=t");
        try
        {
            Assert.That(
                () => new PostgresBrokerConsumer(factory, Opts, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("logger"));
        }
        finally { factory.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }

    [Test]
    public async Task DisposeAsync_NoSubscriptions_DoesNotThrow()
    {
        var factory = new PostgresConnectionFactory("Host=localhost;Database=eip;Username=t;Password=t");
        try
        {
            var consumer = new PostgresBrokerConsumer(factory, Opts, Logger);
            await consumer.DisposeAsync();
            Assert.Pass();
        }
        finally { await factory.DisposeAsync(); }
    }

    [Test]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        var factory = new PostgresConnectionFactory("Host=localhost;Database=eip;Username=t;Password=t");
        try
        {
            var consumer = new PostgresBrokerConsumer(factory, Opts, Logger);
            await consumer.DisposeAsync();
            await consumer.DisposeAsync();
            Assert.Pass();
        }
        finally { await factory.DisposeAsync(); }
    }
}
