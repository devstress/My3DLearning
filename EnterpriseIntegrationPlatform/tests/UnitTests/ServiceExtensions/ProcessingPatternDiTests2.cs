using System.Text.Json;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.CompetingConsumers;
using EnterpriseIntegrationPlatform.Processing.Dispatcher;
using EnterpriseIntegrationPlatform.Processing.RequestReply;
using EnterpriseIntegrationPlatform.Processing.ScatterGather;
using EnterpriseIntegrationPlatform.Processing.Throttle;
using EnterpriseIntegrationPlatform.Processing.Transform;
using EnterpriseIntegrationPlatform.Processing.Translator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.ServiceExtensions;

[TestFixture]
public class ProcessingPatternDiTests2
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
    public void AddTransformPipeline_RegistersService()
    {
        _services.AddTransformPipeline(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ITransformPipeline>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddJsonToXmlStep_RegistersService()
    {
        _services.AddJsonToXmlStep();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ITransformStep>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddXmlToJsonStep_RegistersService()
    {
        _services.AddXmlToJsonStep();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ITransformStep>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddContentEnricher_RegistersService()
    {
        _services.AddContentEnricher(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IContentEnricher>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddContentFilter_RegistersService()
    {
        _services.AddContentFilter();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IContentFilter>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddNormalizer_RegistersService()
    {
        _services.AddNormalizer(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<INormalizer>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddJsonMessageTranslator_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddJsonMessageTranslator(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IMessageTranslator<JsonElement, JsonElement>>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddMessageDispatcher_RegistersService()
    {
        _services.AddMessageDispatcher(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IMessageDispatcher>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddServiceActivator_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddServiceActivator(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IServiceActivator>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddScatterGather_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddScatterGather<string, string>(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IScatterGatherer<string, string>>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddRequestReplyCorrelator_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddSingleton(Substitute.For<IMessageBrokerConsumer>());
        _services.AddRequestReplyCorrelator<string, string>(_config);
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<IRequestReplyCorrelator<string, string>>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddCompetingConsumers_RegistersConsumerLagMonitor()
    {
        _services.AddCompetingConsumers(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IConsumerLagMonitor>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddCompetingConsumers_RegistersBackpressureSignal()
    {
        _services.AddCompetingConsumers(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IBackpressureSignal>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddCompetingConsumers_RegistersConsumerScaler()
    {
        _services.AddCompetingConsumers(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IConsumerScaler>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddMessageThrottle_RegistersMessageThrottle()
    {
        _services.AddMessageThrottle(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IMessageThrottle>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddMessageThrottle_RegistersThrottleRegistry()
    {
        _services.AddMessageThrottle(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IThrottleRegistry>();

        Assert.That(svc, Is.Not.Null);
    }
}
