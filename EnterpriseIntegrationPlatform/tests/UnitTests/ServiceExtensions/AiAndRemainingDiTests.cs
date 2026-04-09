using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.AI.Ollama;
using EnterpriseIntegrationPlatform.AI.RagFlow;
using EnterpriseIntegrationPlatform.AI.RagKnowledge;
using EnterpriseIntegrationPlatform.RuleEngine;
using EnterpriseIntegrationPlatform.Security.Secrets;
using EnterpriseIntegrationPlatform.Workflow.Temporal;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.ServiceExtensions;

[TestFixture]
public class AiAndRemainingDiTests
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
    public void AddOllamaService_RegistersService()
    {
        _services.AddOllamaService();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IOllamaService>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddRagFlowService_RegistersService()
    {
        _services.AddRagFlowService(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IRagFlowService>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddRagKnowledge_RegistersDocumentParser()
    {
        _services.AddRagKnowledge();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<RagDocumentParser>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddRagKnowledge_RegistersKnowledgeIndex()
    {
        _services.AddRagKnowledge();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<RagKnowledgeIndex>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddRagKnowledge_RegistersQueryMatcher()
    {
        _services.AddRagKnowledge();
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<RagQueryMatcher>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddSecretsManagement_RegistersSecretProvider()
    {
        _services.AddSecretsManagement(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ISecretProvider>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddSecretsManagement_RegistersSecretRotationService()
    {
        _services.AddSecretsManagement(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ISecretRotationService>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddRuleEngine_RegistersRuleEngine()
    {
        _services.AddRuleEngine(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IRuleEngine>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddRuleEngine_RegistersRuleStore()
    {
        _services.AddRuleEngine(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IRuleStore>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddTemporalWorkflows_RegistersMessageValidationService()
    {
        _services.AddTemporalWorkflows(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IMessageValidationService>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddTemporalWorkflows_RegistersMessageLoggingService()
    {
        _services.AddTemporalWorkflows(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<IMessageLoggingService>();

        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AddTemporalWorkflows_RegistersCompensationActivityService()
    {
        _services.AddTemporalWorkflows(_config);
        var provider = _services.BuildServiceProvider();

        var svc = provider.GetService<ICompensationActivityService>();

        Assert.That(svc, Is.Not.Null);
    }
}
