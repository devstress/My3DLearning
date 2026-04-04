using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class ConnectorRegistryTests
{
    private ConnectorRegistry _registry = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = new ConnectorRegistry();
    }

    private static IConnector CreateMockConnector(
        string name,
        ConnectorType type = ConnectorType.Http)
    {
        var connector = Substitute.For<IConnector>();
        connector.Name.Returns(name);
        connector.ConnectorType.Returns(type);
        return connector;
    }

    [Test]
    public void Register_ValidConnector_IncreasesCount()
    {
        var connector = CreateMockConnector("http-api");

        _registry.Register(connector);

        Assert.That(_registry.Count, Is.EqualTo(1));
    }

    [Test]
    public void Register_NullConnector_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _registry.Register(null!));
    }

    [Test]
    public void Register_DuplicateName_OverwritesPrevious()
    {
        var first = CreateMockConnector("api-v1", ConnectorType.Http);
        var second = CreateMockConnector("api-v1", ConnectorType.Sftp);

        _registry.Register(first);
        _registry.Register(second);

        Assert.That(_registry.Count, Is.EqualTo(1));
        Assert.That(_registry.GetByName("api-v1")!.ConnectorType, Is.EqualTo(ConnectorType.Sftp));
    }

    [Test]
    public void Remove_ExistingConnector_ReturnsTrue()
    {
        _registry.Register(CreateMockConnector("to-remove"));

        var removed = _registry.Remove("to-remove");

        Assert.That(removed, Is.True);
        Assert.That(_registry.Count, Is.EqualTo(0));
    }

    [Test]
    public void Remove_NonExistentName_ReturnsFalse()
    {
        var removed = _registry.Remove("does-not-exist");

        Assert.That(removed, Is.False);
    }

    [Test]
    public void GetByName_ExistingConnector_ReturnsConnector()
    {
        var connector = CreateMockConnector("order-api");
        _registry.Register(connector);

        var result = _registry.GetByName("order-api");

        Assert.That(result, Is.SameAs(connector));
    }

    [Test]
    public void GetByName_CaseInsensitive_ReturnsConnector()
    {
        var connector = CreateMockConnector("order-api");
        _registry.Register(connector);

        var result = _registry.GetByName("ORDER-API");

        Assert.That(result, Is.SameAs(connector));
    }

    [Test]
    public void GetByName_NonExistentName_ReturnsNull()
    {
        var result = _registry.GetByName("unknown");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetByType_MultipleConnectors_ReturnsOnlyMatchingType()
    {
        _registry.Register(CreateMockConnector("http-1", ConnectorType.Http));
        _registry.Register(CreateMockConnector("sftp-1", ConnectorType.Sftp));
        _registry.Register(CreateMockConnector("http-2", ConnectorType.Http));

        var httpConnectors = _registry.GetByType(ConnectorType.Http);

        Assert.That(httpConnectors, Has.Count.EqualTo(2));
        Assert.That(httpConnectors.Select(c => c.Name), Is.EquivalentTo(new[] { "http-1", "http-2" }));
    }

    [Test]
    public void GetByType_NoMatchingType_ReturnsEmptyList()
    {
        _registry.Register(CreateMockConnector("http-1", ConnectorType.Http));

        var result = _registry.GetByType(ConnectorType.Email);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetAll_MultipleConnectors_ReturnsAll()
    {
        _registry.Register(CreateMockConnector("a", ConnectorType.Http));
        _registry.Register(CreateMockConnector("b", ConnectorType.Sftp));
        _registry.Register(CreateMockConnector("c", ConnectorType.Email));

        var all = _registry.GetAll();

        Assert.That(all, Has.Count.EqualTo(3));
    }

    [Test]
    public void GetDescriptors_PopulatesAllFields()
    {
        var connector = CreateMockConnector("my-conn", ConnectorType.File);
        _registry.Register(connector);

        var descriptors = _registry.GetDescriptors();

        Assert.That(descriptors, Has.Count.EqualTo(1));
        Assert.That(descriptors[0].Name, Is.EqualTo("my-conn"));
        Assert.That(descriptors[0].ConnectorType, Is.EqualTo(ConnectorType.File));
        Assert.That(descriptors[0].ImplementationType, Is.Not.Null);
    }

    [Test]
    public void Count_EmptyRegistry_ReturnsZero()
    {
        Assert.That(_registry.Count, Is.EqualTo(0));
    }
}

[TestFixture]
public class ConnectorFactoryTests
{
    private IConnectorRegistry _registry = null!;
    private ConnectorFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = Substitute.For<IConnectorRegistry>();
        _factory = new ConnectorFactory(_registry, NullLogger<ConnectorFactory>.Instance);
    }

    [Test]
    public void Create_ExistingConnector_ReturnsConnector()
    {
        var connector = Substitute.For<IConnector>();
        connector.Name.Returns("order-api");
        _registry.GetByName("order-api").Returns(connector);

        var result = _factory.Create("order-api");

        Assert.That(result, Is.SameAs(connector));
    }

    [Test]
    public void Create_NonExistentConnector_ThrowsInvalidOperationException()
    {
        _registry.GetByName("missing").Returns((IConnector?)null);
        _registry.GetAll().Returns(Array.Empty<IConnector>());

        Assert.Throws<InvalidOperationException>(() => _factory.Create("missing"));
    }

    [Test]
    public void TryCreate_ExistingConnector_ReturnsTrueAndConnector()
    {
        var connector = Substitute.For<IConnector>();
        _registry.GetByName("found").Returns(connector);

        var success = _factory.TryCreate("found", out var result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.SameAs(connector));
    }

    [Test]
    public void TryCreate_NonExistentConnector_ReturnsFalse()
    {
        _registry.GetByName("missing").Returns((IConnector?)null);

        var success = _factory.TryCreate("missing", out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Create_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _factory.Create(null!));
    }

    [Test]
    public void TryCreate_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _factory.TryCreate("", out _));
    }
}

[TestFixture]
public class ConnectorHealthAggregatorTests
{
    private IConnectorRegistry _registry = null!;
    private ConnectorHealthAggregator _aggregator = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = Substitute.For<IConnectorRegistry>();
        _aggregator = new ConnectorHealthAggregator(
            _registry, NullLogger<ConnectorHealthAggregator>.Instance);
    }

    private static IConnector CreateConnectorWithHealth(string name, bool healthy)
    {
        var connector = Substitute.For<IConnector>();
        connector.Name.Returns(name);
        connector.TestConnectionAsync(Arg.Any<CancellationToken>()).Returns(healthy);
        return connector;
    }

    [Test]
    public async Task CheckHealthAsync_AllHealthy_ReturnsHealthy()
    {
        var connectors = new[]
        {
            CreateConnectorWithHealth("api-1", healthy: true),
            CreateConnectorWithHealth("sftp-1", healthy: true),
        };
        _registry.GetAll().Returns(connectors);

        var result = await _aggregator.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        Assert.That(result.Description, Does.Contain("2"));
    }

    [Test]
    public async Task CheckHealthAsync_AllUnhealthy_ReturnsUnhealthy()
    {
        var connectors = new[]
        {
            CreateConnectorWithHealth("api-1", healthy: false),
            CreateConnectorWithHealth("sftp-1", healthy: false),
        };
        _registry.GetAll().Returns(connectors);

        var result = await _aggregator.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
    }

    [Test]
    public async Task CheckHealthAsync_SomeUnhealthy_ReturnsDegraded()
    {
        var connectors = new[]
        {
            CreateConnectorWithHealth("api-1", healthy: true),
            CreateConnectorWithHealth("sftp-1", healthy: false),
        };
        _registry.GetAll().Returns(connectors);

        var result = await _aggregator.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Degraded));
    }

    [Test]
    public async Task CheckHealthAsync_NoConnectors_ReturnsUnhealthy()
    {
        _registry.GetAll().Returns(Array.Empty<IConnector>());

        var result = await _aggregator.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Description, Does.Contain("No connectors"));
    }

    [Test]
    public async Task CheckHealthAsync_ConnectorThrows_TreatedAsUnhealthy()
    {
        var throwing = Substitute.For<IConnector>();
        throwing.Name.Returns("buggy");
        throwing.TestConnectionAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("connection failed"));

        var healthy = CreateConnectorWithHealth("good", healthy: true);
        _registry.GetAll().Returns(new[] { throwing, healthy });

        var result = await _aggregator.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Degraded));
        Assert.That(result.Data!["buggy"], Is.EqualTo("Unhealthy"));
        Assert.That(result.Data!["good"], Is.EqualTo("Healthy"));
    }

    [Test]
    public async Task CheckHealthAsync_HealthyResult_IncludesDataPerConnector()
    {
        var connectors = new[]
        {
            CreateConnectorWithHealth("http-api", healthy: true),
            CreateConnectorWithHealth("sftp-vendor", healthy: true),
        };
        _registry.GetAll().Returns(connectors);

        var result = await _aggregator.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        Assert.That(result.Data, Has.Count.EqualTo(2));
        Assert.That(result.Data!["http-api"], Is.EqualTo("Healthy"));
        Assert.That(result.Data!["sftp-vendor"], Is.EqualTo("Healthy"));
    }
}
