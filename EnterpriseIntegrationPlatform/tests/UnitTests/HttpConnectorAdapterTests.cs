using System.Text.Json;
using EnterpriseIntegrationPlatform.Connector.Http;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class HttpConnectorAdapterTests
{
    private sealed record TestPayload(string Value);

    private IHttpConnector _httpConnector = null!;
    private HttpConnectorAdapter _adapter = null!;

    private static IntegrationEnvelope<TestPayload> BuildEnvelope() =>
        IntegrationEnvelope<TestPayload>.Create(new TestPayload("test"), "UnitTest", "TestEvent");

    [SetUp]
    public void SetUp()
    {
        _httpConnector = Substitute.For<IHttpConnector>();
        var options = Options.Create(new HttpConnectorOptions { BaseUrl = "https://api.example.com" });
        _adapter = new HttpConnectorAdapter(
            "http-test",
            _httpConnector,
            options,
            NullLogger<HttpConnectorAdapter>.Instance);
    }

    [Test]
    public void Name_ReturnsConfiguredName()
    {
        Assert.That(_adapter.Name, Is.EqualTo("http-test"));
    }

    [Test]
    public void ConnectorType_IsHttp()
    {
        Assert.That(_adapter.ConnectorType, Is.EqualTo(ConnectorType.Http));
    }

    [Test]
    public async Task SendAsync_Success_ReturnsOkResult()
    {
        _httpConnector.SendAsync<TestPayload, JsonElement>(
            Arg.Any<IntegrationEnvelope<TestPayload>>(),
            Arg.Any<string>(),
            Arg.Any<HttpMethod>(),
            Arg.Any<CancellationToken>())
            .Returns(new JsonElement());

        var options = new ConnectorSendOptions { Destination = "/api/orders" };
        var result = await _adapter.SendAsync(BuildEnvelope(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("http-test"));
        Assert.That(result.StatusMessage, Does.Contain("POST /api/orders succeeded"));
    }

    [Test]
    public async Task SendAsync_NullDestination_UsesSlash()
    {
        _httpConnector.SendAsync<TestPayload, JsonElement>(
            Arg.Any<IntegrationEnvelope<TestPayload>>(),
            Arg.Is("/"),
            Arg.Any<HttpMethod>(),
            Arg.Any<CancellationToken>())
            .Returns(new JsonElement());

        var options = new ConnectorSendOptions(); // no Destination
        var result = await _adapter.SendAsync(BuildEnvelope(), options);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task SendAsync_Exception_ReturnsFailResult()
    {
        _httpConnector.SendAsync<TestPayload, JsonElement>(
            Arg.Any<IntegrationEnvelope<TestPayload>>(),
            Arg.Any<string>(),
            Arg.Any<HttpMethod>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var options = new ConnectorSendOptions { Destination = "/api/fail" };
        var result = await _adapter.SendAsync(BuildEnvelope(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Connection refused"));
    }

    [Test]
    public void SendAsync_NullEnvelope_Throws()
    {
        var options = new ConnectorSendOptions();
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _adapter.SendAsync<TestPayload>(null!, options));
    }

    [Test]
    public void SendAsync_NullOptions_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _adapter.SendAsync(BuildEnvelope(), null!));
    }

    [Test]
    public void Constructor_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HttpConnectorAdapter(
                null!,
                _httpConnector,
                Options.Create(new HttpConnectorOptions()),
                NullLogger<HttpConnectorAdapter>.Instance));
    }

    [Test]
    public void Constructor_NullConnector_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HttpConnectorAdapter(
                "test",
                null!,
                Options.Create(new HttpConnectorOptions()),
                NullLogger<HttpConnectorAdapter>.Instance));
    }
}
