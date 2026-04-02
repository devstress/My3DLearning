using System.Diagnostics;

namespace EnterpriseIntegrationPlatform.Gateway.Api.Middleware;

/// <summary>
/// Middleware that ensures every request has a correlation ID.
/// If the <c>X-Correlation-Id</c> header is present, its value is reused;
/// otherwise a new GUID is generated.
/// The correlation ID is added to the response headers, <see cref="HttpContext.Items"/>,
/// and <see cref="Activity.Current"/> baggage for distributed tracing.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>The HTTP header name used for correlation IDs.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>The key used to store the correlation ID in <see cref="HttpContext.Items"/>.</summary>
    public const string ItemsKey = "CorrelationId";

    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of <see cref="CorrelationIdMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request, ensuring a correlation ID is present.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        context.Items[ItemsKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        Activity.Current?.AddBaggage(ItemsKey, correlationId);

        await _next(context);
    }
}
