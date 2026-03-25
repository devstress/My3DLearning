using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Connector.Http;
using EnterpriseIntegrationPlatform.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class HttpConnectorTests
{
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public List<HttpRequestMessage> Requests { get; } = new();

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
            _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(_respond(request));
        }
    }

    private sealed record TestPayload(string Value);
    private sealed record TestResponse(string Result);

    private static IntegrationEnvelope<TestPayload> BuildEnvelope(string value = "hello") =>
        IntegrationEnvelope<TestPayload>.Create(new TestPayload(value), "TestService", "TestEvent");

    private static (HttpConnector Connector, FakeHttpMessageHandler Handler) BuildConnector(
        HttpResponseMessage? tokenResponse = null,
        HttpResponseMessage? mainResponse = null,
        HttpConnectorOptions? options = null,
        ITokenCache? tokenCache = null)
    {
        var opts = options ?? new HttpConnectorOptions { BaseUrl = "https://api.example.com" };
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("token"))
                return tokenResponse ?? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"access_token":"test-token"}""")
                };
            return mainResponse ?? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"result":"ok"}""")
            };
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri(opts.BaseUrl) };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("HttpConnector").Returns(client);
        var cache = tokenCache ?? new InMemoryTokenCache();
        var connector = new HttpConnector(
            factory,
            cache,
            Options.Create(opts),
            NullLogger<HttpConnector>.Instance);
        return (connector, handler);
    }

    [Fact]
    public async Task SendAsync_ValidEnvelope_SendsToCorrectUrl()
    {
        var (connector, handler) = BuildConnector();
        var envelope = BuildEnvelope();

        await connector.SendAsync<TestPayload, TestResponse>(envelope, "/orders", HttpMethod.Post, CancellationToken.None);

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/orders");
    }

    [Fact]
    public async Task SendAsync_ValidEnvelope_AddsCorrelationHeaders()
    {
        var (connector, handler) = BuildConnector();
        var envelope = BuildEnvelope();

        await connector.SendAsync<TestPayload, TestResponse>(envelope, "/orders", HttpMethod.Post, CancellationToken.None);

        var request = handler.Requests[0];
        request.Headers.Should().Contain(h => h.Key == "X-Correlation-Id");
        request.Headers.Should().Contain(h => h.Key == "X-Message-Id");
        request.Headers.GetValues("X-Correlation-Id").Should().Contain(envelope.CorrelationId.ToString());
        request.Headers.GetValues("X-Message-Id").Should().Contain(envelope.MessageId.ToString());
    }

    [Fact]
    public async Task SendAsync_GetMethod_DoesNotSendBody()
    {
        var (connector, handler) = BuildConnector();
        var envelope = BuildEnvelope();

        await connector.SendAsync<TestPayload, TestResponse>(envelope, "/orders/1", HttpMethod.Get, CancellationToken.None);

        handler.Requests[0].Content.Should().BeNull();
    }

    [Fact]
    public async Task SendWithTokenAsync_FirstCall_TokenEndpointCalled()
    {
        var (connector, handler) = BuildConnector();
        var envelope = BuildEnvelope();

        await connector.SendWithTokenAsync<TestPayload, TestResponse>(
            envelope, "/orders", HttpMethod.Post,
            "https://api.example.com/token", "grant_type=client_credentials", "Authorization",
            CancellationToken.None);

        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/token");
    }

    [Fact]
    public async Task SendWithTokenAsync_SecondCall_CachedTokenReused()
    {
        var (connector, handler) = BuildConnector();
        var envelope = BuildEnvelope();

        await connector.SendWithTokenAsync<TestPayload, TestResponse>(
            envelope, "/orders", HttpMethod.Post,
            "https://api.example.com/token", "grant_type=client_credentials", "Authorization",
            CancellationToken.None);

        await connector.SendWithTokenAsync<TestPayload, TestResponse>(
            envelope, "/orders", HttpMethod.Post,
            "https://api.example.com/token", "grant_type=client_credentials", "Authorization",
            CancellationToken.None);

        var tokenRequests = handler.Requests
            .Where(r => r.RequestUri!.AbsolutePath == "/token")
            .ToList();
        tokenRequests.Should().HaveCount(1, "cached token should be reused on second call");
    }

    [Fact]
    public async Task SendWithTokenAsync_ExpiredToken_TokenEndpointCalledAgain()
    {
        var tokenCache = Substitute.For<ITokenCache>();
        string? outToken;
        tokenCache.TryGetToken(Arg.Any<string>(), out outToken)
            .Returns(false);

        var (connector, handler) = BuildConnector(tokenCache: tokenCache);
        var envelope = BuildEnvelope();

        await connector.SendWithTokenAsync<TestPayload, TestResponse>(
            envelope, "/orders", HttpMethod.Post,
            "https://api.example.com/token", "grant_type=client_credentials", "Authorization",
            CancellationToken.None);

        await connector.SendWithTokenAsync<TestPayload, TestResponse>(
            envelope, "/orders", HttpMethod.Post,
            "https://api.example.com/token", "grant_type=client_credentials", "Authorization",
            CancellationToken.None);

        var tokenRequests = handler.Requests
            .Where(r => r.RequestUri!.AbsolutePath == "/token")
            .ToList();
        tokenRequests.Should().HaveCount(2, "expired cache should trigger refetch on each call");
    }

    [Fact]
    public async Task SendAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var (connector, _) = BuildConnector();

        var act = async () => await connector.SendAsync<TestPayload, TestResponse>(
            null!, "/orders", HttpMethod.Post, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_EmptyBaseUrl_ThrowsInvalidOperationException()
    {
        var opts = new HttpConnectorOptions { BaseUrl = "" };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());

        var act = () => new HttpConnector(
            factory,
            new InMemoryTokenCache(),
            Options.Create(opts),
            NullLogger<HttpConnector>.Instance);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task SendAsync_PostMethod_SerializesEnvelopeAsBody()
    {
        var (connector, handler) = BuildConnector();
        var envelope = BuildEnvelope("world");

        await connector.SendAsync<TestPayload, TestResponse>(envelope, "/orders", HttpMethod.Post, CancellationToken.None);

        handler.Requests[0].Content.Should().NotBeNull();
        var body = await handler.Requests[0].Content!.ReadAsStringAsync();
        body.Should().Contain("world");
    }

    [Fact]
    public async Task SendAsync_ValidResponse_ReturnsDeserializedResponse()
    {
        var responseJson = """{"result":"deserialized-value"}""";
        var mainResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };
        var (connector, _) = BuildConnector(mainResponse: mainResponse);
        var envelope = BuildEnvelope();

        var result = await connector.SendAsync<TestPayload, TestResponse>(
            envelope, "/items", HttpMethod.Get, CancellationToken.None);

        result.Should().NotBeNull();
        result.Result.Should().Be("deserialized-value");
    }

    [Fact]
    public async Task SendWithTokenAsync_Authorization_SendsBearerHeader()
    {
        var (connector, handler) = BuildConnector();
        var envelope = BuildEnvelope();

        await connector.SendWithTokenAsync<TestPayload, TestResponse>(
            envelope, "/orders", HttpMethod.Post,
            "https://api.example.com/token", "grant_type=client_credentials", "Authorization",
            CancellationToken.None);

        var mainRequest = handler.Requests.Last();
        mainRequest.Headers.Authorization.Should().NotBeNull();
        mainRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        mainRequest.Headers.Authorization!.Parameter.Should().Be("test-token");
    }
}
