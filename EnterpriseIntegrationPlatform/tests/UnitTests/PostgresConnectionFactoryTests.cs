using EnterpriseIntegrationPlatform.Ingestion.Postgres;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PostgresConnectionFactoryTests
{
    [Test]
    public void Constructor_NullConnectionString_Throws()
    {
        Assert.That(
            () => new PostgresConnectionFactory(null!),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Constructor_EmptyConnectionString_Throws()
    {
        Assert.That(
            () => new PostgresConnectionFactory(""),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Constructor_WhitespaceConnectionString_Throws()
    {
        Assert.That(
            () => new PostgresConnectionFactory("   "),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Constructor_ValidConnectionString_DoesNotThrow()
    {
        PostgresConnectionFactory? factory = null;
        Assert.That(
            () => factory = new PostgresConnectionFactory("Host=localhost;Database=eip;Username=test;Password=test"),
            Throws.Nothing);

        factory?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
