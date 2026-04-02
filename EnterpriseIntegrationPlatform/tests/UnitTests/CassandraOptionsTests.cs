using EnterpriseIntegrationPlatform.Storage.Cassandra;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class CassandraOptionsTests
{
    [Test]
    public void DefaultContactPoints_IsLocalhost()
    {
        var options = new CassandraOptions();

        Assert.That(options.ContactPoints, Is.EqualTo("localhost"));
    }

    [Test]
    public void DefaultPort_Is15042()
    {
        var options = new CassandraOptions();

        Assert.That(options.Port, Is.EqualTo(15042));
    }

    [Test]
    public void DefaultKeyspace_IsEip()
    {
        var options = new CassandraOptions();

        Assert.That(options.Keyspace, Is.EqualTo("eip"));
    }

    [Test]
    public void DefaultReplicationFactor_Is3()
    {
        var options = new CassandraOptions();

        Assert.That(options.ReplicationFactor, Is.EqualTo(3));
    }

    [Test]
    public void DefaultTtlSeconds_Is30Days()
    {
        var options = new CassandraOptions();

        Assert.That(options.DefaultTtlSeconds, Is.EqualTo(2_592_000));
    }

    [Test]
    public void SectionName_IsCassandra()
    {
        Assert.That(CassandraOptions.SectionName, Is.EqualTo("Cassandra"));
    }

    [Test]
    public void ContactPoints_CanBeSetToMultipleHosts()
    {
        var options = new CassandraOptions
        {
            ContactPoints = "node1,node2,node3",
        };

        Assert.That(options.ContactPoints, Is.EqualTo("node1,node2,node3"));
    }

    [Test]
    public void TtlSeconds_CanBeDisabled()
    {
        var options = new CassandraOptions
        {
            DefaultTtlSeconds = 0,
        };

        Assert.That(options.DefaultTtlSeconds, Is.EqualTo(0));
    }
}
