using EnterpriseIntegrationPlatform.AI.Ollama;
using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.DisasterRecovery;
using EnterpriseIntegrationPlatform.EventSourcing;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using EnterpriseIntegrationPlatform.Observability;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using EnterpriseIntegrationPlatform.SystemManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Performance.Profiling;

namespace EnterpriseIntegrationPlatform.Tests.Unit.ServiceExtensions;

[TestFixture]
public class InfrastructureDiTests
{
    private IConfiguration _config = null!;
    private ServiceCollection _services = null!;

    [SetUp]
    public void SetUp()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();

        _services = new ServiceCollection();
        _services.AddLogging();
    }

    [Test]
    public void AddMessagingChannels_RegistersPointToPointChannel()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddSingleton(Substitute.For<IMessageBrokerConsumer>());
        _services.AddMessagingChannels();
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<IPointToPointChannel>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddMessagingChannels_RegistersPublishSubscribeChannel()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddSingleton(Substitute.For<IMessageBrokerConsumer>());
        _services.AddMessagingChannels();
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<IPublishSubscribeChannel>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddDatatypeChannel_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddDatatypeChannel(_config);
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<IDatatypeChannel>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddInvalidMessageChannel_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddInvalidMessageChannel(_config);
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<IInvalidMessageChannel>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public async Task AddMessagingBridge_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddSingleton(Substitute.For<IMessageBrokerConsumer>());
        _services.AddMessagingBridge(_config);
        var provider = _services.BuildServiceProvider();

        var scope = provider.CreateAsyncScope();
        var svc = scope.ServiceProvider.GetService<IMessagingBridge>();

        Assert.That(svc, Is.Not.Null);

        await scope.DisposeAsync();
    }

    [Test]
    public void AddConfigurationManagement_RegistersConfigurationStore()
    {
        _services.AddConfigurationManagement();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IConfigurationStore>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddConfigurationManagement_RegistersFeatureFlagService()
    {
        _services.AddConfigurationManagement();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IFeatureFlagService>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddConfigurationManagement_RegistersChangeNotifier()
    {
        _services.AddConfigurationManagement();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ConfigurationChangeNotifier>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddConfigurationManagement_RegistersEnvironmentOverrideProvider()
    {
        _services.AddConfigurationManagement();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<EnvironmentOverrideProvider>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddEventSourcing_RegistersEventStore()
    {
        _services.AddEventSourcing<Dictionary<string, object>, TestProjection>(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IEventStore>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddEventSourcing_RegistersSnapshotStore()
    {
        _services.AddEventSourcing<Dictionary<string, object>, TestProjection>(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ISnapshotStore<Dictionary<string, object>>>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddEventSourcing_RegistersProjectionEngine()
    {
        _services.AddEventSourcing<Dictionary<string, object>, TestProjection>(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<EventProjectionEngine<Dictionary<string, object>>>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddControlBus_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddSingleton(Substitute.For<IMessageBrokerConsumer>());
        _services.AddControlBus(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IControlBus>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddMessageStore_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageRepository>());
        _services.AddMessageStore();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IMessageStore>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddSmartProxy_RegistersService()
    {
        _services.AddSmartProxy();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ISmartProxy>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddTestMessageGenerator_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddTestMessageGenerator();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ITestMessageGenerator>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddDisasterRecovery_RegistersFailoverManager()
    {
        _services.AddDisasterRecovery(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IFailoverManager>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddDisasterRecovery_RegistersReplicationManager()
    {
        _services.AddDisasterRecovery(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IReplicationManager>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddDisasterRecovery_RegistersRecoveryPointValidator()
    {
        _services.AddDisasterRecovery(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IRecoveryPointValidator>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddDisasterRecovery_RegistersDrDrillRunner()
    {
        _services.AddDisasterRecovery(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IDrDrillRunner>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddPlatformObservability_RegistersMessageStateStore()
    {
        _services.AddSingleton(Substitute.For<IOllamaService>());
        _services.AddPlatformObservability("http://localhost:15100");
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IMessageStateStore>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddPlatformObservability_RegistersLifecycleRecorder()
    {
        _services.AddSingleton(Substitute.For<IOllamaService>());
        _services.AddPlatformObservability("http://localhost:15100");
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<MessageLifecycleRecorder>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddPlatformObservability_RegistersTraceAnalyzer()
    {
        _services.AddSingleton(Substitute.For<IOllamaService>());
        _services.AddPlatformObservability("http://localhost:15100");
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ITraceAnalyzer>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddPerformanceProfiling_RegistersContinuousProfiler()
    {
        _services.AddPerformanceProfiling(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IContinuousProfiler>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddPerformanceProfiling_RegistersHotspotDetector()
    {
        _services.AddPerformanceProfiling(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IHotspotDetector>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddPerformanceProfiling_RegistersGcMonitor()
    {
        _services.AddPerformanceProfiling(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IGcMonitor>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddPerformanceProfiling_RegistersBenchmarkRegistry()
    {
        _services.AddPerformanceProfiling(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IBenchmarkRegistry>();

        Assert.That(svc, Is.Not.Null);
    }

    private sealed class TestProjection : IEventProjection<Dictionary<string, object>>
    {
        public Task<Dictionary<string, object>> ProjectAsync(
            Dictionary<string, object> state,
            EventEnvelope envelope,
            CancellationToken cancellationToken = default)
            => Task.FromResult(state);
    }
}
