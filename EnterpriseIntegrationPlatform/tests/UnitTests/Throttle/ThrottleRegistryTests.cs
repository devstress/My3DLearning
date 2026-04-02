using EnterpriseIntegrationPlatform.Processing.Throttle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.Throttle;

[TestFixture]
public sealed class ThrottleRegistryTests
{
    private ILoggerFactory _loggerFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>())
            .Returns(Substitute.For<ILogger>());
    }

    [TearDown]
    public void TearDown()
    {
        _loggerFactory.Dispose();
    }

    [Test]
    public void Constructor_CreatesGlobalPolicy()
    {
        using var registry = CreateRegistry();

        var policies = registry.GetAllPolicies();

        Assert.That(policies, Has.Count.EqualTo(1));
        Assert.That(policies[0].Policy.PolicyId, Is.EqualTo("global"));
    }

    [Test]
    public void SetPolicy_CreatesTenantScopedThrottle()
    {
        using var registry = CreateRegistry();

        registry.SetPolicy(new ThrottlePolicy
        {
            PolicyId = "tenant-acme",
            Name = "Acme Corp Throttle",
            Partition = new ThrottlePartitionKey { TenantId = "acme" },
            MaxMessagesPerSecond = 50,
            BurstCapacity = 100,
        });

        var policies = registry.GetAllPolicies();
        Assert.That(policies, Has.Count.EqualTo(2));
        Assert.That(policies.Any(p => p.Policy.PolicyId == "tenant-acme"), Is.True);
    }

    [Test]
    public void SetPolicy_CreatesQueueScopedThrottle()
    {
        using var registry = CreateRegistry();

        registry.SetPolicy(new ThrottlePolicy
        {
            PolicyId = "queue-orders",
            Name = "Orders Queue Throttle",
            Partition = new ThrottlePartitionKey { Queue = "orders.inbound" },
            MaxMessagesPerSecond = 200,
            BurstCapacity = 400,
        });

        var policy = registry.GetPolicy("queue-orders");
        Assert.That(policy, Is.Not.Null);
        Assert.That(policy!.Policy.MaxMessagesPerSecond, Is.EqualTo(200));
    }

    [Test]
    public void SetPolicy_CreatesEndpointScopedThrottle()
    {
        using var registry = CreateRegistry();

        registry.SetPolicy(new ThrottlePolicy
        {
            PolicyId = "endpoint-sap",
            Name = "SAP Endpoint Throttle",
            Partition = new ThrottlePartitionKey { Endpoint = "sap-api.example.com" },
            MaxMessagesPerSecond = 10,
            BurstCapacity = 20,
        });

        var policy = registry.GetPolicy("endpoint-sap");
        Assert.That(policy, Is.Not.Null);
        Assert.That(policy!.Policy.MaxMessagesPerSecond, Is.EqualTo(10));
    }

    [Test]
    public void SetPolicy_UpdatesExistingPolicy()
    {
        using var registry = CreateRegistry();

        registry.SetPolicy(new ThrottlePolicy
        {
            PolicyId = "tenant-acme",
            Name = "Acme v1",
            Partition = new ThrottlePartitionKey { TenantId = "acme" },
            MaxMessagesPerSecond = 50,
            BurstCapacity = 100,
        });

        registry.SetPolicy(new ThrottlePolicy
        {
            PolicyId = "tenant-acme",
            Name = "Acme v2",
            Partition = new ThrottlePartitionKey { TenantId = "acme" },
            MaxMessagesPerSecond = 200,
            BurstCapacity = 400,
        });

        var policy = registry.GetPolicy("tenant-acme");
        Assert.That(policy!.Policy.MaxMessagesPerSecond, Is.EqualTo(200));
        // Should still have 2 policies (global + acme), not 3.
        Assert.That(registry.GetAllPolicies(), Has.Count.EqualTo(2));
    }

    [Test]
    public void RemovePolicy_RemovesNonGlobalPolicy()
    {
        using var registry = CreateRegistry();

        registry.SetPolicy(new ThrottlePolicy
        {
            PolicyId = "temp",
            Name = "Temporary",
            Partition = new ThrottlePartitionKey { TenantId = "temp" },
        });

        var removed = registry.RemovePolicy("temp");
        Assert.That(removed, Is.True);
        Assert.That(registry.GetPolicy("temp"), Is.Null);
    }

    [Test]
    public void RemovePolicy_CannotRemoveGlobalPolicy()
    {
        using var registry = CreateRegistry();

        var removed = registry.RemovePolicy("global");

        Assert.That(removed, Is.False);
        Assert.That(registry.GetPolicy("global"), Is.Not.Null);
    }

    [Test]
    public void Resolve_TenantSpecificPartition_ReturnsTenantThrottle()
    {
        using var registry = CreateRegistry();

        registry.SetPolicy(new ThrottlePolicy
        {
            PolicyId = "tenant-acme",
            Name = "Acme",
            Partition = new ThrottlePartitionKey { TenantId = "acme" },
            MaxMessagesPerSecond = 50,
            BurstCapacity = 50,
        });

        var throttle = registry.Resolve(new ThrottlePartitionKey { TenantId = "acme" });

        // Tenant throttle has burst=50 vs global default 200.
        Assert.That(throttle.AvailableTokens, Is.EqualTo(50));
    }

    [Test]
    public void Resolve_UnknownPartition_FallsBackToGlobal()
    {
        using var registry = CreateRegistry();

        var throttle = registry.Resolve(new ThrottlePartitionKey { TenantId = "unknown" });

        // Global default has burst=200.
        Assert.That(throttle.AvailableTokens, Is.EqualTo(200));
    }

    [Test]
    public void Resolve_TenantPlusQueuePartition_MatchesSpecific()
    {
        using var registry = CreateRegistry();

        registry.SetPolicy(new ThrottlePolicy
        {
            PolicyId = "acme-orders",
            Name = "Acme Orders",
            Partition = new ThrottlePartitionKey { TenantId = "acme", Queue = "orders" },
            MaxMessagesPerSecond = 25,
            BurstCapacity = 25,
        });

        var throttle = registry.Resolve(
            new ThrottlePartitionKey { TenantId = "acme", Queue = "orders", Endpoint = "api" });

        Assert.That(throttle.AvailableTokens, Is.EqualTo(25));
    }

    [Test]
    public void GetPolicy_UnknownId_ReturnsNull()
    {
        using var registry = CreateRegistry();

        Assert.That(registry.GetPolicy("nonexistent"), Is.Null);
    }

    [Test]
    public void GetAllPolicies_IncludesMetrics()
    {
        using var registry = CreateRegistry();

        var policies = registry.GetAllPolicies();

        Assert.That(policies[0].Metrics, Is.Not.Null);
        Assert.That(policies[0].Metrics.BurstCapacity, Is.EqualTo(200));
        Assert.That(policies[0].Metrics.RefillRate, Is.EqualTo(100));
    }

    private ThrottleRegistry CreateRegistry()
    {
        var options = Options.Create(new ThrottleOptions
        {
            MaxMessagesPerSecond = 100,
            BurstCapacity = 200,
            MaxWaitTime = TimeSpan.FromSeconds(30),
        });

        return new ThrottleRegistry(options, _loggerFactory);
    }
}
