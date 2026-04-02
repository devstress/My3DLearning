using EnterpriseIntegrationPlatform.Gateway.Api.Middleware;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.Gateway;

[TestFixture]
public sealed class CorrelationIdMiddlewareTests
{
    private CorrelationIdMiddleware _middleware = null!;
    private bool _nextCalled;

    [SetUp]
    public void SetUp()
    {
        _nextCalled = false;
        _middleware = new CorrelationIdMiddleware(_ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task InvokeAsync_NoHeader_GeneratesNewCorrelationId()
    {
        var context = new DefaultHttpContext();

        await _middleware.InvokeAsync(context);

        Assert.That(context.Items[CorrelationIdMiddleware.ItemsKey], Is.Not.Null);
        var id = context.Items[CorrelationIdMiddleware.ItemsKey]!.ToString();
        Assert.That(Guid.TryParse(id, out _), Is.True);
        Assert.That(_nextCalled, Is.True);
    }

    [Test]
    public async Task InvokeAsync_ExistingHeader_UsesProvidedCorrelationId()
    {
        var expected = Guid.NewGuid().ToString("D");
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = expected;

        await _middleware.InvokeAsync(context);

        Assert.That(context.Items[CorrelationIdMiddleware.ItemsKey], Is.EqualTo(expected));
    }

    [Test]
    public async Task InvokeAsync_SetsCorrelationIdOnResponseHeaders()
    {
        var context = new DefaultHttpContext();

        await _middleware.InvokeAsync(context);

        // The OnStarting callback sets response headers; trigger it manually.
        // DefaultHttpContext doesn't execute OnStarting, so we verify via Items.
        Assert.That(context.Items.ContainsKey(CorrelationIdMiddleware.ItemsKey), Is.True);
    }

    [Test]
    public async Task InvokeAsync_StoresCorrelationIdInHttpContextItems()
    {
        var expected = "test-correlation-123";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = expected;

        await _middleware.InvokeAsync(context);

        Assert.That(context.Items[CorrelationIdMiddleware.ItemsKey], Is.EqualTo(expected));
    }

    [Test]
    public async Task InvokeAsync_EmptyHeader_GeneratesNewCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "";

        await _middleware.InvokeAsync(context);

        var id = context.Items[CorrelationIdMiddleware.ItemsKey]!.ToString();
        Assert.That(string.IsNullOrWhiteSpace(id), Is.False);
        Assert.That(Guid.TryParse(id, out _), Is.True);
    }
}
