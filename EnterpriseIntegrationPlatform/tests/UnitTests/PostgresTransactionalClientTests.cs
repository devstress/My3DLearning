using EnterpriseIntegrationPlatform.Ingestion.Postgres;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PostgresTransactionalClientTests
{
    private static readonly NullLogger<PostgresTransactionalClient> Logger = NullLogger<PostgresTransactionalClient>.Instance;

    [Test]
    public void Constructor_NullFactory_Throws()
    {
        Assert.That(
            () => new PostgresTransactionalClient(null!, Logger),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("factory"));
    }

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        var factory = new PostgresConnectionFactory("Host=localhost;Database=eip;Username=t;Password=t");
        try
        {
            Assert.That(
                () => new PostgresTransactionalClient(factory, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("logger"));
        }
        finally { factory.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }

    [Test]
    public void SupportsNativeTransactions_ReturnsTrue()
    {
        var factory = new PostgresConnectionFactory("Host=localhost;Database=eip;Username=t;Password=t");
        try
        {
            var client = new PostgresTransactionalClient(factory, Logger);

            Assert.That(client.SupportsNativeTransactions, Is.True);
        }
        finally { factory.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }
}
