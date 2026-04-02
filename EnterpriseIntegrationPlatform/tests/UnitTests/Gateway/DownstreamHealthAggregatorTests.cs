using System.Net;
using EnterpriseIntegrationPlatform.Gateway.Api.Configuration;
using EnterpriseIntegrationPlatform.Gateway.Api.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.Gateway;

[TestFixture]
public sealed class DownstreamHealthAggregatorTests
{
    private GatewayOptions _gatewayOptions = null!;
    private ILogger<DownstreamHealthAggregator> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _gatewayOptions = new GatewayOptions
        {
            AdminApiBaseUrl = "http://admin-api:5200",
            OpenClawBaseUrl = "http://openclaw:5100",
        };
        _logger = Substitute.For<ILogger<DownstreamHealthAggregator>>();
    }

    [Test]
    public async Task CheckHealthAsync_AllHealthy_ReturnsHealthy()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var client = new HttpClient(handler);
        var options = Options.Create(_gatewayOptions);
        var aggregator = new DownstreamHealthAggregator(client, options, _logger);

        var result = await aggregator.CheckHealthAsync(
            new HealthCheckContext { Registration = new HealthCheckRegistration("test", aggregator, null, null) });

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
    }

    [Test]
    public async Task CheckHealthAsync_OneUnhealthy_ReturnsDegraded()
    {
        var handler = new FakeHttpMessageHandler(new Dictionary<string, HttpStatusCode>
        {
            ["admin-api"] = HttpStatusCode.OK,
            ["openclaw"] = HttpStatusCode.ServiceUnavailable,
        });
        var client = new HttpClient(handler);
        var options = Options.Create(_gatewayOptions);
        var aggregator = new DownstreamHealthAggregator(client, options, _logger);

        var result = await aggregator.CheckHealthAsync(
            new HealthCheckContext { Registration = new HealthCheckRegistration("test", aggregator, null, null) });

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Degraded));
    }

    [Test]
    public async Task CheckHealthAsync_AllUnhealthy_ReturnsUnhealthy()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.ServiceUnavailable);
        var client = new HttpClient(handler);
        var options = Options.Create(_gatewayOptions);
        var aggregator = new DownstreamHealthAggregator(client, options, _logger);

        var result = await aggregator.CheckHealthAsync(
            new HealthCheckContext { Registration = new HealthCheckRegistration("test", aggregator, null, null) });

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
    }

    /// <summary>
    /// A fake HTTP message handler that returns a fixed status code.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _defaultStatusCode;
        private readonly Dictionary<string, HttpStatusCode>? _perHostStatusCodes;

        public FakeHttpMessageHandler(HttpStatusCode statusCode)
        {
            _defaultStatusCode = statusCode;
        }

        public FakeHttpMessageHandler(Dictionary<string, HttpStatusCode> perHostStatusCodes)
        {
            _perHostStatusCodes = perHostStatusCodes;
            _defaultStatusCode = HttpStatusCode.OK;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var statusCode = _defaultStatusCode;
            if (_perHostStatusCodes is not null && request.RequestUri is not null)
            {
                var host = request.RequestUri.Host;
                foreach (var (key, value) in _perHostStatusCodes)
                {
                    if (host.Contains(key, StringComparison.OrdinalIgnoreCase))
                    {
                        statusCode = value;
                        break;
                    }
                }
            }

            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
