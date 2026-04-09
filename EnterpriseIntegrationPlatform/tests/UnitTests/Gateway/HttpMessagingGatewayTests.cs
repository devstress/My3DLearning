using System.Net;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Gateway.Api;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.Gateway;

[TestFixture]
public class HttpMessagingGatewayTests
{
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;
        public List<HttpRequestMessage> CapturedRequests { get; } = [];

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) =>
            _respond = respond;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
            : this((req, _) => Task.FromResult(respond(req)))
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            CapturedRequests.Add(request);
            return _respond(request, ct);
        }
    }

    private sealed record TestPayload(string Value);
    private sealed record TestResponse(string Result);

    private static HttpMessagingGateway BuildGateway(
        FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("downstream").Returns(httpClient);
        return new HttpMessagingGateway(factory, NullLogger<HttpMessagingGateway>.Instance);
    }

    // ── SendAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task SendAsync_Success_ReturnsSuccessWithCorrelationId()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var gateway = BuildGateway(handler);
        var result = await gateway.SendAsync("http://localhost/api/orders", new TestPayload("test"));

        Assert.That(result.Success, Is.True);
        Assert.That(result.CorrelationId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task SendAsync_Success_SendsPostWithJsonContent()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var gateway = BuildGateway(handler);
        await gateway.SendAsync("http://localhost/api/orders", new TestPayload("hello"));

        Assert.That(handler.CapturedRequests, Has.Count.EqualTo(1));
        var req = handler.CapturedRequests[0];
        Assert.That(req.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(req.RequestUri!.ToString(), Is.EqualTo("http://localhost/api/orders"));
        Assert.That(req.Content, Is.Not.Null);
    }

    [Test]
    public async Task SendAsync_Success_SetsCorrelationIdHeader()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var gateway = BuildGateway(handler);
        var result = await gateway.SendAsync("http://localhost/test", new TestPayload("x"));

        var req = handler.CapturedRequests[0];
        Assert.That(req.Headers.Contains("X-Correlation-Id"), Is.True);
        var headerValue = req.Headers.GetValues("X-Correlation-Id").First();
        Assert.That(Guid.TryParse(headerValue, out var parsed), Is.True);
        Assert.That(parsed, Is.EqualTo(result.CorrelationId));
    }

    [Test]
    public async Task SendAsync_Success_ForwardsCustomHeaders()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var gateway = BuildGateway(handler);
        var headers = new Dictionary<string, string>
        {
            ["X-Custom-Header"] = "custom-value",
            ["X-Tenant-Id"] = "tenant-42",
        };
        await gateway.SendAsync("http://localhost/test", new TestPayload("x"), headers);

        var req = handler.CapturedRequests[0];
        Assert.That(req.Headers.GetValues("X-Custom-Header").First(), Is.EqualTo("custom-value"));
        Assert.That(req.Headers.GetValues("X-Tenant-Id").First(), Is.EqualTo("tenant-42"));
    }

    [Test]
    public async Task SendAsync_NonSuccessStatus_ReturnsFailure()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad request body"),
            });

        var gateway = BuildGateway(handler);
        var result = await gateway.SendAsync("http://localhost/fail", new TestPayload("x"));

        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error, Is.EqualTo("Bad request body"));
    }

    [Test]
    public async Task SendAsync_HttpRequestException_Returns502()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var gateway = BuildGateway(handler);
        var result = await gateway.SendAsync("http://localhost/down", new TestPayload("x"));

        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(502));
        Assert.That(result.Error, Does.Contain("Connection refused"));
    }

    [Test]
    public async Task SendAsync_Timeout_Returns504()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            throw new TaskCanceledException("timeout", new TimeoutException()));

        var gateway = BuildGateway(handler);
        var result = await gateway.SendAsync("http://localhost/slow", new TestPayload("x"));

        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(504));
        Assert.That(result.Error, Does.Contain("timeout"));
    }

    [Test]
    public void SendAsync_NullDestination_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var gateway = BuildGateway(handler);
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await gateway.SendAsync<TestPayload>(null!, new TestPayload("x")));
    }

    [Test]
    public void SendAsync_NullPayload_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var gateway = BuildGateway(handler);
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await gateway.SendAsync<TestPayload>("http://localhost/test", null!));
    }

    // ── SendAndReceiveAsync ───────────────────────────────────────────────

    [Test]
    public async Task SendAndReceiveAsync_Success_ReturnsPayload()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new TestResponse("ok")),
                    System.Text.Encoding.UTF8,
                    "application/json"),
            });

        var gateway = BuildGateway(handler);
        var result = await gateway.SendAndReceiveAsync<TestPayload, TestResponse>(
            "http://localhost/api/echo", new TestPayload("ping"));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Payload, Is.Not.Null);
        Assert.That(result.Payload!.Result, Is.EqualTo("ok"));
        Assert.That(result.CorrelationId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task SendAndReceiveAsync_NonSuccessStatus_ReturnsFailureWithError()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server error details"),
            });

        var gateway = BuildGateway(handler);
        var result = await gateway.SendAndReceiveAsync<TestPayload, TestResponse>(
            "http://localhost/api/fail", new TestPayload("x"));

        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(500));
        Assert.That(result.Error, Is.EqualTo("Server error details"));
        Assert.That(result.Payload, Is.Null);
    }

    [Test]
    public async Task SendAndReceiveAsync_HttpRequestException_Returns502()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var gateway = BuildGateway(handler);
        var result = await gateway.SendAndReceiveAsync<TestPayload, TestResponse>(
            "http://localhost/down", new TestPayload("x"));

        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(502));
        Assert.That(result.Error, Does.Contain("Connection refused"));
    }

    [Test]
    public async Task SendAndReceiveAsync_Timeout_Returns504()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            throw new TaskCanceledException("timeout", new TimeoutException()));

        var gateway = BuildGateway(handler);
        var result = await gateway.SendAndReceiveAsync<TestPayload, TestResponse>(
            "http://localhost/slow", new TestPayload("x"));

        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(504));
        Assert.That(result.Error, Does.Contain("timeout"));
    }

    [Test]
    public void SendAndReceiveAsync_NullDestination_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var gateway = BuildGateway(handler);
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await gateway.SendAndReceiveAsync<TestPayload, TestResponse>(null!, new TestPayload("x")));
    }

    [Test]
    public void SendAndReceiveAsync_NullRequest_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var gateway = BuildGateway(handler);
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await gateway.SendAndReceiveAsync<TestPayload, TestResponse>("http://localhost/test", null!));
    }
}
