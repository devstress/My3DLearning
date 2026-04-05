using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class RoutingServiceExtensionsTests
{
    private IConfiguration _config = null!;
    private ServiceCollection _services = null!;

    [SetUp]
    public void SetUp()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RouterOptions.SectionName}:DefaultTopic"] = "default.topic",
                [$"{DynamicRouterOptions.SectionName}:FallbackTopic"] = "fallback.topic",
                [$"{RecipientListOptions.SectionName}:MetadataRecipientsKey"] = "recipients",
                [$"{MessageFilterOptions.SectionName}:OutputTopic"] = "filter.output",
                [$"{DetourOptions.SectionName}:DetourTopic"] = "detour.topic",
                [$"{DetourOptions.SectionName}:OutputTopic"] = "output.topic",
            })
            .Build();

        _services = new ServiceCollection();
        _services.AddLogging();
        _services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
    }

    [Test]
    public void AddContentBasedRouter_RegistersService()
    {
        _services.AddContentBasedRouter(_config);
        var provider = _services.BuildServiceProvider();

        var router = provider.GetService<IContentBasedRouter>();

        Assert.That(router, Is.Not.Null);
    }

    [Test]
    public void AddDynamicRouter_RegistersIDynamicRouter()
    {
        _services.AddDynamicRouter(_config);
        var provider = _services.BuildServiceProvider();

        var router = provider.GetService<IDynamicRouter>();

        Assert.That(router, Is.Not.Null);
    }

    [Test]
    public void AddDynamicRouter_RegistersIRouterControlChannel()
    {
        _services.AddDynamicRouter(_config);
        var provider = _services.BuildServiceProvider();

        var channel = provider.GetService<IRouterControlChannel>();

        Assert.That(channel, Is.Not.Null);
    }

    [Test]
    public void AddDynamicRouter_SameInstance()
    {
        _services.AddDynamicRouter(_config);
        var provider = _services.BuildServiceProvider();

        var router = provider.GetRequiredService<IDynamicRouter>();
        var channel = provider.GetRequiredService<IRouterControlChannel>();

        Assert.That(router, Is.SameAs(channel));
    }

    [Test]
    public void AddRecipientList_RegistersService()
    {
        _services.AddRecipientList(_config);
        var provider = _services.BuildServiceProvider();

        var list = provider.GetService<IRecipientList>();

        Assert.That(list, Is.Not.Null);
    }

    [Test]
    public void AddMessageFilter_RegistersService()
    {
        _services.AddMessageFilter(_config);
        var provider = _services.BuildServiceProvider();

        var filter = provider.GetService<IMessageFilter>();

        Assert.That(filter, Is.Not.Null);
    }

    [Test]
    public void AddDetour_RegistersService()
    {
        _services.AddDetour(_config);
        var provider = _services.BuildServiceProvider();

        var detour = provider.GetService<IDetour>();

        Assert.That(detour, Is.Not.Null);
    }
}
