using System.Threading.RateLimiting;
using EnterpriseIntegrationPlatform.Gateway.Api.Configuration;
using EnterpriseIntegrationPlatform.Gateway.Api.Health;
using EnterpriseIntegrationPlatform.Gateway.Api.Routing;
using EnterpriseIntegrationPlatform.Security;

namespace EnterpriseIntegrationPlatform.Gateway.Api;

/// <summary>
/// Extension methods for registering API Gateway services in the DI container.
/// </summary>
public static class GatewayServiceExtensions
{
    /// <summary>
    /// Registers all API Gateway services including routing, health checks,
    /// rate limiting, JWT authentication, and CORS.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // ── Options ───────────────────────────────────────────────────────────
        services.Configure<GatewayOptions>(
            configuration.GetSection(GatewayOptions.SectionName));

        var gatewayOptions = configuration
            .GetSection(GatewayOptions.SectionName)
            .Get<GatewayOptions>() ?? new GatewayOptions();

        // ── Routing ───────────────────────────────────────────────────────────
        services.AddSingleton<IRouteResolver, RouteResolver>();

        // ── Health Checks ─────────────────────────────────────────────────────
        services.AddHttpClient<DownstreamHealthAggregator>();
        services.AddHealthChecks()
            .AddCheck<DownstreamHealthAggregator>(
                "downstream-services",
                tags: ["ready"]);

        // ── Authentication ────────────────────────────────────────────────────
        services.AddPlatformJwtAuthentication(configuration);
        services.AddAuthorization();

        // ── Rate Limiting ─────────────────────────────────────────────────────
        // Per-client fixed window (by IP) and global sliding window are combined
        // via a chained PartitionedRateLimiter on the global limiter.
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                // Per-client: fixed window keyed by client IP
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        clientIp,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = gatewayOptions.RateLimitPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                        });
                }),
                // Global: sliding window shared across all clients
                PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        "global",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = gatewayOptions.GlobalRateLimitPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                        })));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        // ── CORS ──────────────────────────────────────────────────────────────
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (gatewayOptions.AllowedOrigins is ["*"])
                {
                    policy.AllowAnyOrigin();
                }
                else
                {
                    policy.WithOrigins(gatewayOptions.AllowedOrigins);
                }

                policy.AllowAnyHeader().AllowAnyMethod();
            });
        });

        return services;
    }
}
