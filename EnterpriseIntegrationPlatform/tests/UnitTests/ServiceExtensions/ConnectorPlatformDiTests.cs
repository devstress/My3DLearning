using EnterpriseIntegrationPlatform.Connector.Email;
using EnterpriseIntegrationPlatform.Connector.FileSystem;
using EnterpriseIntegrationPlatform.Connector.Http;
using EnterpriseIntegrationPlatform.Connector.Sftp;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.MultiTenancy;
using EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.ServiceExtensions;

[TestFixture]
public class ConnectorPlatformDiTests
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
    public void AddConnectors_RegistersConnectorRegistry()
    {
        _services.AddConnectors();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IConnectorRegistry>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddConnectors_RegistersConnectorFactory()
    {
        _services.AddConnectors();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IConnectorFactory>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddConnectorHealthCheck_DoesNotThrow()
    {
        _services.AddConnectors();
        _services.AddConnectorHealthCheck();

        Assert.DoesNotThrow(() => _services.BuildServiceProvider());
    }

    [Test]
    public void AddHttpConnector_RegistersHttpConnector()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HttpConnector:BaseUrl"] = "http://localhost",
            })
            .Build();
        _services.AddHttpConnector(config);
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<IHttpConnector>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddHttpConnector_RegistersTokenCache()
    {
        _services.AddHttpConnector(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ITokenCache>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddEmailConnector_RegistersEmailConnector()
    {
        _services.AddEmailConnector(_config);
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<IEmailConnector>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddEmailConnector_RegistersSmtpClientWrapper()
    {
        _services.AddEmailConnector(_config);
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<ISmtpClientWrapper>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddSftpConnector_RegistersSftpConnector()
    {
        _services.AddSftpConnector(_config);
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<ISftpConnector>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddSftpConnector_RegistersSftpConnectionPool()
    {
        _services.AddSftpConnector(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ISftpConnectionPool>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddFileConnector_RegistersFileConnector()
    {
        _services.AddFileConnector(_config);
        var provider = _services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<IFileConnector>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddFileConnector_RegistersFileSystem()
    {
        _services.AddFileConnector(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IFileSystem>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddMultiTenancy_RegistersTenantResolver()
    {
        _services.AddMultiTenancy();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ITenantResolver>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddMultiTenancy_RegistersTenantIsolationGuard()
    {
        _services.AddMultiTenancy();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ITenantIsolationGuard>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddTenantOnboarding_RegistersTenantOnboardingService()
    {
        _services.AddTenantOnboarding(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ITenantOnboardingService>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddTenantOnboarding_RegistersTenantQuotaManager()
    {
        _services.AddTenantOnboarding(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ITenantQuotaManager>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddTenantOnboarding_RegistersBrokerNamespaceProvisioner()
    {
        _services.AddTenantOnboarding(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IBrokerNamespaceProvisioner>();

        Assert.That(svc, Is.Not.Null);
    }
}
