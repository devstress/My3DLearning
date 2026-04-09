using System.Text.Json;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Aggregator;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using EnterpriseIntegrationPlatform.Processing.Replay;
using EnterpriseIntegrationPlatform.Processing.Retry;
using EnterpriseIntegrationPlatform.Processing.Splitter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.ServiceExtensions;

[TestFixture]
public class ProcessingPatternDiTests1
{
    private IConfiguration _config = null!;
    private ServiceCollection _services = null!;

    [SetUp]
    public void SetUp()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MessageAggregator:ExpectedCount"] = "5",
            })
            .Build();

        _services = new ServiceCollection();
        _services.AddLogging();
    }

    [Test]
    public void AddDeadLetterPublisher_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddDeadLetterPublisher<string>(_config);
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<IDeadLetterPublisher<string>>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddMessageExpirationChecker_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IDeadLetterPublisher<string>>());
        _services.AddSingleton(TimeProvider.System);
        _services.AddMessageExpirationChecker<string>();
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<IMessageExpirationChecker<string>>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddRetryPolicy_RegistersService()
    {
        _services.AddRetryPolicy(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IRetryPolicy>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddMessageReplay_RegistersMessageReplayStore()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddMessageReplay(_config);
        var provider = _services.BuildServiceProvider();

        var store = provider.GetService<IMessageReplayStore>();

        Assert.That(store, Is.Not.Null);
    }

    [Test]
    public void AddMessageReplay_RegistersMessageReplayer()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddMessageReplay(_config);
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var replayer = scope.ServiceProvider.GetService<IMessageReplayer>();

        Assert.That(replayer, Is.Not.Null);
    }

    [Test]
    public void AddMessageSplitter_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddMessageSplitter<string>(_config, s => new List<string> { s });
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IMessageSplitter<string>>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddJsonMessageSplitter_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddJsonMessageSplitter(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IMessageSplitter<JsonElement>>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddMessageAggregator_RegistersService()
    {
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        _services.AddMessageAggregator<string, string>(
            _config,
            items => string.Join(",", items));
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IMessageAggregator<string, string>>();

        Assert.That(svc, Is.Not.Null);
    }
}
