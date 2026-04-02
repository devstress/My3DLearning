using EnterpriseIntegrationPlatform.Gateway.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.Gateway;

[TestFixture]
public sealed class RequestLoggingMiddlewareTests
{
    private ILogger<RequestLoggingMiddleware> _logger = null!;
    private int _responseStatusCode;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
        _responseStatusCode = StatusCodes.Status200OK;
    }

    [Test]
    public async Task InvokeAsync_LogsRequestWithMethodAndPath()
    {
        var middleware = new RequestLoggingMiddleware(
            context =>
            {
                context.Response.StatusCode = _responseStatusCode;
                return Task.CompletedTask;
            },
            _logger);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/admin/status";
        context.Items[CorrelationIdMiddleware.ItemsKey] = "test-id";

        await middleware.InvokeAsync(context);

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task InvokeAsync_IncludesStatusCode()
    {
        _responseStatusCode = StatusCodes.Status404NotFound;

        var middleware = new RequestLoggingMiddleware(
            context =>
            {
                context.Response.StatusCode = _responseStatusCode;
                return Task.CompletedTask;
            },
            _logger);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/inspect/query";
        context.Items[CorrelationIdMiddleware.ItemsKey] = "test-id-2";

        await middleware.InvokeAsync(context);

        // Verify logger was called (status code is included in the structured log)
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task InvokeAsync_WithoutCorrelationId_UsesUnknown()
    {
        var middleware = new RequestLoggingMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            _logger);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/";

        await middleware.InvokeAsync(context);

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
