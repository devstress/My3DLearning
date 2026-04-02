using EnterpriseIntegrationPlatform.Processing.Throttle;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.Throttle;

[TestFixture]
public sealed class ThrottlePartitionKeyTests
{
    [Test]
    public void ToKey_GlobalPartition_ReturnsWildcards()
    {
        var key = ThrottlePartitionKey.Global;

        Assert.That(key.ToKey(), Is.EqualTo("tenant:*|queue:*|endpoint:*"));
    }

    [Test]
    public void ToKey_TenantOnly_IncludesTenantId()
    {
        var key = new ThrottlePartitionKey { TenantId = "acme" };

        Assert.That(key.ToKey(), Is.EqualTo("tenant:acme|queue:*|endpoint:*"));
    }

    [Test]
    public void ToKey_QueueOnly_IncludesQueueName()
    {
        var key = new ThrottlePartitionKey { Queue = "orders.inbound" };

        Assert.That(key.ToKey(), Is.EqualTo("tenant:*|queue:orders.inbound|endpoint:*"));
    }

    [Test]
    public void ToKey_FullPartition_IncludesAllDimensions()
    {
        var key = new ThrottlePartitionKey
        {
            TenantId = "acme",
            Queue = "orders",
            Endpoint = "sap-api",
        };

        Assert.That(key.ToKey(), Is.EqualTo("tenant:acme|queue:orders|endpoint:sap-api"));
    }

    [Test]
    public void Global_IsNotResolved_DefaultInstance()
    {
        Assert.That(ThrottlePartitionKey.Global.TenantId, Is.Null);
        Assert.That(ThrottlePartitionKey.Global.Queue, Is.Null);
        Assert.That(ThrottlePartitionKey.Global.Endpoint, Is.Null);
    }
}
