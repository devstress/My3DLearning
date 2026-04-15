using EnterpriseIntegrationPlatform.MultiTenancy;
using EnterpriseIntegrationPlatform.Processing.Throttle;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public sealed class ThrottlePolicyTests
{
    [Test]
    public void Defaults_AreCorrect()
    {
        var policy = new ThrottlePolicy
        {
            PolicyId = "test",
            Name = "Test Policy",
            Partition = ThrottlePartitionKey.Global,
        };

        Assert.That(policy.MaxMessagesPerSecond, Is.EqualTo(100));
        Assert.That(policy.BurstCapacity, Is.EqualTo(200));
        Assert.That(policy.MaxWaitTime, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(policy.RejectOnBackpressure, Is.False);
        Assert.That(policy.IsEnabled, Is.True);
        Assert.That(policy.LastModifiedUtc, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }

    [Test]
    public void SettableProperties_RoundTrip()
    {
        var policy = new ThrottlePolicy
        {
            PolicyId = "custom",
            Name = "Custom",
            Partition = new ThrottlePartitionKey { TenantId = "acme" },
            MaxMessagesPerSecond = 50,
            BurstCapacity = 75,
            MaxWaitTime = TimeSpan.FromSeconds(10),
            RejectOnBackpressure = true,
            IsEnabled = false,
        };

        Assert.That(policy.PolicyId, Is.EqualTo("custom"));
        Assert.That(policy.Name, Is.EqualTo("Custom"));
        Assert.That(policy.Partition.TenantId, Is.EqualTo("acme"));
        Assert.That(policy.MaxMessagesPerSecond, Is.EqualTo(50));
        Assert.That(policy.BurstCapacity, Is.EqualTo(75));
        Assert.That(policy.MaxWaitTime.TotalSeconds, Is.EqualTo(10));
        Assert.That(policy.RejectOnBackpressure, Is.True);
        Assert.That(policy.IsEnabled, Is.False);
    }
}

[TestFixture]
public sealed class ThrottleResultTests
{
    [Test]
    public void Permitted_Result_HasNoRejectionReason()
    {
        var result = new ThrottleResult
        {
            Permitted = true,
            WaitTime = TimeSpan.FromMilliseconds(5),
            RemainingTokens = 195,
        };

        Assert.That(result.Permitted, Is.True);
        Assert.That(result.WaitTime.TotalMilliseconds, Is.EqualTo(5));
        Assert.That(result.RemainingTokens, Is.EqualTo(195));
        Assert.That(result.RejectionReason, Is.Null);
    }

    [Test]
    public void Rejected_Result_HasReason()
    {
        var result = new ThrottleResult
        {
            Permitted = false,
            WaitTime = TimeSpan.FromSeconds(30),
            RemainingTokens = 0,
            RejectionReason = "Backpressure: no tokens available",
        };

        Assert.That(result.Permitted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("Backpressure: no tokens available"));
    }

    [Test]
    public void Record_Equality_SameValues_AreEqual()
    {
        var a = new ThrottleResult { Permitted = true, WaitTime = TimeSpan.Zero, RemainingTokens = 10 };
        var b = new ThrottleResult { Permitted = true, WaitTime = TimeSpan.Zero, RemainingTokens = 10 };

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Record_Equality_DifferentValues_AreNotEqual()
    {
        var a = new ThrottleResult { Permitted = true, WaitTime = TimeSpan.Zero, RemainingTokens = 10 };
        var b = new ThrottleResult { Permitted = false, WaitTime = TimeSpan.Zero, RemainingTokens = 0 };

        Assert.That(a, Is.Not.EqualTo(b));
    }
}

[TestFixture]
public sealed class ThrottleMetricsTests
{
    [Test]
    public void AllProperties_SetCorrectly()
    {
        var metrics = new ThrottleMetrics
        {
            TotalAcquired = 1000,
            TotalRejected = 5,
            AvailableTokens = 180,
            BurstCapacity = 200,
            RefillRate = 100,
            TotalWaitTime = TimeSpan.FromSeconds(42),
        };

        Assert.That(metrics.TotalAcquired, Is.EqualTo(1000));
        Assert.That(metrics.TotalRejected, Is.EqualTo(5));
        Assert.That(metrics.AvailableTokens, Is.EqualTo(180));
        Assert.That(metrics.BurstCapacity, Is.EqualTo(200));
        Assert.That(metrics.RefillRate, Is.EqualTo(100));
        Assert.That(metrics.TotalWaitTime.TotalSeconds, Is.EqualTo(42));
    }

    [Test]
    public void Record_Equality()
    {
        var a = new ThrottleMetrics
        {
            TotalAcquired = 10, TotalRejected = 0, AvailableTokens = 5,
            BurstCapacity = 10, RefillRate = 5, TotalWaitTime = TimeSpan.Zero
        };
        var b = new ThrottleMetrics
        {
            TotalAcquired = 10, TotalRejected = 0, AvailableTokens = 5,
            BurstCapacity = 10, RefillRate = 5, TotalWaitTime = TimeSpan.Zero
        };

        Assert.That(a, Is.EqualTo(b));
    }
}

[TestFixture]
public sealed class ThrottlePolicyStatusTests
{
    [Test]
    public void PolicyAndMetrics_AreAccessible()
    {
        var policy = new ThrottlePolicy
        {
            PolicyId = "global",
            Name = "Global",
            Partition = ThrottlePartitionKey.Global,
        };

        var metrics = new ThrottleMetrics
        {
            TotalAcquired = 500,
            TotalRejected = 2,
            AvailableTokens = 198,
            BurstCapacity = 200,
            RefillRate = 100,
            TotalWaitTime = TimeSpan.FromSeconds(1),
        };

        var status = new ThrottlePolicyStatus { Policy = policy, Metrics = metrics };

        Assert.That(status.Policy.PolicyId, Is.EqualTo("global"));
        Assert.That(status.Metrics.TotalAcquired, Is.EqualTo(500));
    }
}

[TestFixture]
public sealed class TenantContextTests
{
    [Test]
    public void Anonymous_HasCorrectValues()
    {
        var anon = TenantContext.Anonymous;

        Assert.That(anon.TenantId, Is.EqualTo("anonymous"));
        Assert.That(anon.IsResolved, Is.False);
        Assert.That(anon.TenantName, Is.Null);
    }

    [Test]
    public void Resolved_HasCorrectValues()
    {
        var ctx = new TenantContext
        {
            TenantId = "acme",
            TenantName = "Acme Corp",
            IsResolved = true,
        };

        Assert.That(ctx.TenantId, Is.EqualTo("acme"));
        Assert.That(ctx.TenantName, Is.EqualTo("Acme Corp"));
        Assert.That(ctx.IsResolved, Is.True);
    }

    [Test]
    public void Anonymous_IsSingletonInstance()
    {
        var first = TenantContext.Anonymous;
        var second = TenantContext.Anonymous;
        Assert.That(first, Is.SameAs(second));
    }

    [Test]
    public void Default_TenantName_IsNull()
    {
        var ctx = new TenantContext { TenantId = "test" };
        Assert.That(ctx.TenantName, Is.Null);
    }

    [Test]
    public void Default_IsResolved_IsFalse()
    {
        var ctx = new TenantContext { TenantId = "test" };
        Assert.That(ctx.IsResolved, Is.False);
    }
}

[TestFixture]
public sealed class TenantIsolationExceptionTests
{
    [Test]
    public void Properties_SetCorrectly()
    {
        var msgId = Guid.NewGuid();
        var ex = new TenantIsolationException(msgId, "tenant-a", "tenant-b", "Cross-tenant access denied");

        Assert.That(ex.MessageId, Is.EqualTo(msgId));
        Assert.That(ex.ActualTenantId, Is.EqualTo("tenant-a"));
        Assert.That(ex.ExpectedTenantId, Is.EqualTo("tenant-b"));
        Assert.That(ex.Message, Is.EqualTo("Cross-tenant access denied"));
    }

    [Test]
    public void NullActualTenant_IsAllowed()
    {
        var ex = new TenantIsolationException(Guid.NewGuid(), null, "tenant-b", "No tenant on envelope");

        Assert.That(ex.ActualTenantId, Is.Null);
        Assert.That(ex.ExpectedTenantId, Is.EqualTo("tenant-b"));
    }

    [Test]
    public void IsException_Inherits()
    {
        var ex = new TenantIsolationException(Guid.NewGuid(), "a", "b", "detail");
        Assert.That(ex, Is.InstanceOf<Exception>());
    }

    [Test]
    public void Message_MatchesDetailParameter()
    {
        var ex = new TenantIsolationException(Guid.NewGuid(), "x", "y", "Custom detail message");
        Assert.That(ex.Message, Is.EqualTo("Custom detail message"));
    }
}
