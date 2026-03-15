using EnterpriseIntegrationPlatform.Storage.Cassandra;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class CassandraOptionsTests
{
    [Fact]
    public void DefaultContactPoints_IsLocalhost()
    {
        var options = new CassandraOptions();

        options.ContactPoints.Should().Be("localhost");
    }

    [Fact]
    public void DefaultPort_Is15042()
    {
        var options = new CassandraOptions();

        options.Port.Should().Be(15042);
    }

    [Fact]
    public void DefaultKeyspace_IsEip()
    {
        var options = new CassandraOptions();

        options.Keyspace.Should().Be("eip");
    }

    [Fact]
    public void DefaultReplicationFactor_Is3()
    {
        var options = new CassandraOptions();

        options.ReplicationFactor.Should().Be(3);
    }

    [Fact]
    public void DefaultTtlSeconds_Is30Days()
    {
        var options = new CassandraOptions();

        options.DefaultTtlSeconds.Should().Be(2_592_000);
    }

    [Fact]
    public void SectionName_IsCassandra()
    {
        CassandraOptions.SectionName.Should().Be("Cassandra");
    }

    [Fact]
    public void ContactPoints_CanBeSetToMultipleHosts()
    {
        var options = new CassandraOptions
        {
            ContactPoints = "node1,node2,node3",
        };

        options.ContactPoints.Should().Be("node1,node2,node3");
    }

    [Fact]
    public void TtlSeconds_CanBeDisabled()
    {
        var options = new CassandraOptions
        {
            DefaultTtlSeconds = 0,
        };

        options.DefaultTtlSeconds.Should().Be(0);
    }
}
