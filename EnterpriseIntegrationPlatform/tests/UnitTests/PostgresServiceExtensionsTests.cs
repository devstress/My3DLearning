using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PostgresServiceExtensionsTests
{
    private const string ValidConnectionString = "Host=localhost;Database=eip;Username=test;Password=test";

    private ServiceCollection _services = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
        _services.AddLogging();
    }

    [Test]
    public async Task AddPostgresBroker_RegistersProducer()
    {
        _services.AddPostgresBroker(ValidConnectionString);
        await using var provider = _services.BuildServiceProvider();

        var producer = provider.GetService<IMessageBrokerProducer>();

        Assert.That(producer, Is.Not.Null);
        Assert.That(producer, Is.InstanceOf<PostgresBrokerProducer>());
    }

    [Test]
    public async Task AddPostgresBroker_RegistersConsumer()
    {
        _services.AddPostgresBroker(ValidConnectionString);
        await using var provider = _services.BuildServiceProvider();

        var consumer = provider.GetService<IMessageBrokerConsumer>();

        Assert.That(consumer, Is.Not.Null);
        Assert.That(consumer, Is.InstanceOf<PostgresBrokerConsumer>());
    }

    [Test]
    public async Task AddPostgresBroker_RegistersTransactionalClient()
    {
        _services.AddPostgresBroker(ValidConnectionString);
        await using var provider = _services.BuildServiceProvider();

        var client = provider.GetService<ITransactionalClient>();

        Assert.That(client, Is.Not.Null);
        Assert.That(client, Is.InstanceOf<PostgresTransactionalClient>());
    }

    [Test]
    public async Task AddPostgresBroker_RegistersOptions()
    {
        _services.AddPostgresBroker(ValidConnectionString);
        await using var provider = _services.BuildServiceProvider();

        var options = provider.GetService<IOptions<PostgresBrokerOptions>>();

        Assert.That(options, Is.Not.Null);
        Assert.That(options!.Value.ConnectionString, Is.EqualTo(ValidConnectionString));
    }

    [Test]
    public void AddPostgresBroker_NullConnectionString_Throws()
    {
        Assert.That(
            () => _services.AddPostgresBroker(null!),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void AddPostgresBroker_EmptyConnectionString_Throws()
    {
        Assert.That(
            () => _services.AddPostgresBroker(""),
            Throws.InstanceOf<ArgumentException>());
    }
}
