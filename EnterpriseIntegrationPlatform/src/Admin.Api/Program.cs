using System.Threading.RateLimiting;
using EnterpriseIntegrationPlatform.Admin.Api;
using EnterpriseIntegrationPlatform.Admin.Api.Authentication;
using EnterpriseIntegrationPlatform.Admin.Api.Services;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using EnterpriseIntegrationPlatform.Processing.Replay;
using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.Processing.Throttle;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ── Options ───────────────────────────────────────────────────────────────────
builder.Services.Configure<AdminApiOptions>(
    builder.Configuration.GetSection(AdminApiOptions.SectionName));

// ── Authentication – API key scheme ───────────────────────────────────────────
// All Admin API endpoints are protected by the X-Api-Key header.
// API keys are stored in AdminApi:ApiKeys configuration (never in source code).
builder.Services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

// ── Rate Limiting – fixed window per API key ───────────────────────────────────
// Rate limiting is evaluated before authentication to protect against DoS.
// The partition key is the API key header value (or remote IP as fallback).
var rateLimitPerMinute = builder.Configuration.GetValue("AdminApi:RateLimitPerMinute", 60);
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var apiKey = context.Request.Headers["X-Api-Key"].ToString();
        var partitionKey = string.IsNullOrEmpty(apiKey)
            ? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : apiKey;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── Storage ───────────────────────────────────────────────────────────────────
// Cassandra provides durable, distributed message and fault persistence.
// The health check is automatically registered by AddCassandraStorage().
builder.Services.AddCassandraStorage(builder.Configuration);

// ── Observability ─────────────────────────────────────────────────────────────
// Loki is the durable observability event log. BaseAddress comes from Aspire
// environment variable injection (Loki__BaseAddress) or falls back for local dev.
var lokiBaseAddress = builder.Configuration["Loki:BaseAddress"] ?? "http://localhost:15100";
builder.Services.AddPlatformObservability(lokiBaseAddress);

// ── Admin Services ────────────────────────────────────────────────────────────
builder.Services.AddSingleton<PlatformStatusService>();
builder.Services.AddSingleton<AdminAuditLogger>();
builder.Services.AddMessageReplay(builder.Configuration);
builder.Services.AddSingleton<DlqManagementService>();

// ── Throttle Registry ─────────────────────────────────────────────────────────
// Partitioned throttles per tenant, queue, and endpoint — controllable at runtime.
builder.Services.AddMessageThrottle(builder.Configuration);

// ── Configuration Management ──────────────────────────────────────────────────
// Centralized config store, feature flags, and change notifications.
builder.Services.AddConfigurationManagement();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ── Platform Status ───────────────────────────────────────────────────────────

app.MapGet("/api/admin/status", async (
    PlatformStatusService statusService,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetPlatformStatus", null, http.User);
    var status = await statusService.GetStatusAsync(ct);
    return Results.Ok(status);
})
.WithName("AdminGetPlatformStatus")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Message Queries ───────────────────────────────────────────────────────────

app.MapGet("/api/admin/messages/correlation/{correlationId:guid}", async (
    Guid correlationId,
    IMessageRepository repository,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("QueryMessagesByCorrelation", correlationId.ToString(), http.User);
    var records = await repository.GetByCorrelationIdAsync(correlationId, ct);
    return Results.Ok(records);
})
.WithName("AdminGetMessagesByCorrelation")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/messages/{messageId:guid}", async (
    Guid messageId,
    IMessageRepository repository,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("QueryMessageById", messageId.ToString(), http.User);
    var record = await repository.GetByMessageIdAsync(messageId, ct);
    return record is null ? Results.NotFound() : Results.Ok(record);
})
.WithName("AdminGetMessageById")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Message Status Update ─────────────────────────────────────────────────────

app.MapPatch("/api/admin/messages/{messageId:guid}/status", async (
    Guid messageId,
    UpdateMessageStatusRequest request,
    IMessageRepository repository,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    if (!Enum.IsDefined(request.Status))
        return Results.BadRequest($"Unknown delivery status '{request.Status}'.");

    audit.LogAction("UpdateMessageStatus", messageId.ToString(), http.User);

    await repository.UpdateDeliveryStatusAsync(
        messageId,
        request.CorrelationId,
        request.RecordedAt,
        request.Status,
        ct);

    return Results.NoContent();
})
.WithName("AdminUpdateMessageStatus")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Fault Queries ─────────────────────────────────────────────────────────────

app.MapGet("/api/admin/faults/correlation/{correlationId:guid}", async (
    Guid correlationId,
    IMessageRepository repository,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("QueryFaultsByCorrelation", correlationId.ToString(), http.User);
    var faults = await repository.GetFaultsByCorrelationIdAsync(correlationId, ct);
    return Results.Ok(faults);
})
.WithName("AdminGetFaultsByCorrelation")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Observability Event Queries ───────────────────────────────────────────────

app.MapGet("/api/admin/events/correlation/{correlationId:guid}", async (
    Guid correlationId,
    IObservabilityEventLog eventLog,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("QueryEventsByCorrelation", correlationId.ToString(), http.User);
    var events = await eventLog.GetByCorrelationIdAsync(correlationId, ct);
    return Results.Ok(events);
})
.WithName("AdminGetEventsByCorrelation")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/events/business/{businessKey}", async (
    string businessKey,
    IObservabilityEventLog eventLog,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("QueryEventsByBusinessKey", businessKey, http.User);
    var events = await eventLog.GetByBusinessKeyAsync(businessKey, ct);
    return Results.Ok(events);
})
.WithName("AdminGetEventsByBusinessKey")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── DLQ Management ────────────────────────────────────────────────────────────

app.MapPost("/api/admin/dlq/resubmit", async (
    DlqResubmitRequest request,
    DlqManagementService dlqService,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("DlqResubmit", request.CorrelationId?.ToString(), http.User);

    var filter = new ReplayFilter
    {
        CorrelationId = request.CorrelationId,
        MessageType = request.MessageType,
        FromTimestamp = request.FromTimestamp,
        ToTimestamp = request.ToTimestamp,
    };

    var result = await dlqService.ResubmitAsync(filter, ct);
    return Results.Ok(result);
})
.WithName("AdminDlqResubmit")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Throttle Management ───────────────────────────────────────────────────────
// Admin-controlled throttle policies per tenant, queue, and endpoint.
// Like BizTalk host throttling and Camel per-route throttle EIP.

app.MapGet("/api/admin/throttle/policies", (
    IThrottleRegistry registry,
    AdminAuditLogger audit,
    HttpContext http) =>
{
    audit.LogAction("GetThrottlePolicies", null, http.User);
    var policies = registry.GetAllPolicies();
    return Results.Ok(policies);
})
.WithName("AdminGetThrottlePolicies")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/throttle/policies/{policyId}", (
    string policyId,
    IThrottleRegistry registry,
    AdminAuditLogger audit,
    HttpContext http) =>
{
    audit.LogAction("GetThrottlePolicy", policyId, http.User);
    var policy = registry.GetPolicy(policyId);
    return policy is null ? Results.NotFound() : Results.Ok(policy);
})
.WithName("AdminGetThrottlePolicy")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapPut("/api/admin/throttle/policies", (
    SetThrottlePolicyRequest request,
    IThrottleRegistry registry,
    AdminAuditLogger audit,
    HttpContext http) =>
{
    audit.LogAction("SetThrottlePolicy", request.PolicyId, http.User);

    var policy = new ThrottlePolicy
    {
        PolicyId = request.PolicyId,
        Name = request.Name,
        Partition = new ThrottlePartitionKey
        {
            TenantId = request.TenantId,
            Queue = request.Queue,
            Endpoint = request.Endpoint,
        },
        MaxMessagesPerSecond = request.MaxMessagesPerSecond,
        BurstCapacity = request.BurstCapacity,
        MaxWaitTime = TimeSpan.FromSeconds(request.MaxWaitTimeSeconds),
        RejectOnBackpressure = request.RejectOnBackpressure,
        IsEnabled = request.IsEnabled,
        LastModifiedUtc = DateTimeOffset.UtcNow,
    };

    registry.SetPolicy(policy);
    return Results.Ok(policy);
})
.WithName("AdminSetThrottlePolicy")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapDelete("/api/admin/throttle/policies/{policyId}", (
    string policyId,
    IThrottleRegistry registry,
    AdminAuditLogger audit,
    HttpContext http) =>
{
    audit.LogAction("DeleteThrottlePolicy", policyId, http.User);
    var removed = registry.RemovePolicy(policyId);
    return removed ? Results.NoContent() : Results.NotFound();
})
.WithName("AdminDeleteThrottlePolicy")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Rate Limit Status ─────────────────────────────────────────────────────────
// Exposes current rate limiter configuration for admin visibility.

app.MapGet("/api/admin/ratelimit/status", (
    AdminAuditLogger audit,
    HttpContext http) =>
{
    audit.LogAction("GetRateLimitStatus", null, http.User);
    return Results.Ok(new
    {
        AdminApi = new
        {
            Type = "FixedWindow",
            PermitLimit = rateLimitPerMinute,
            WindowMinutes = 1,
            PartitionBy = "X-Api-Key header (fallback: client IP)",
            RejectionStatusCode = 429,
        },
        Description = "Rate limiting rejects excess HTTP requests with 429. Throttling (see /api/admin/throttle/policies) controls message processing throughput by delaying — they are independent mechanisms.",
    });
})
.WithName("AdminGetRateLimitStatus")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Configuration Management ──────────────────────────────────────────────────
// Centralized config store with environment overrides and change notifications.

app.MapGet("/api/admin/config", async (
    string? environment,
    IConfigurationStore configStore,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("ListConfiguration", environment, http.User);
    var entries = await configStore.ListAsync(environment, ct);
    return Results.Ok(entries);
})
.WithName("AdminListConfiguration")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/config/{key}", async (
    string key,
    string? environment,
    IConfigurationStore configStore,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetConfiguration", key, http.User);
    var entry = await configStore.GetAsync(key, environment ?? "default", ct);
    return entry is null ? Results.NotFound() : Results.Ok(entry);
})
.WithName("AdminGetConfiguration")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapPut("/api/admin/config/{key}", async (
    string key,
    SetConfigurationRequest request,
    IConfigurationStore configStore,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("SetConfiguration", key, http.User);
    var entry = new ConfigurationEntry(
        Key: key,
        Value: request.Value,
        Environment: request.Environment ?? "default",
        ModifiedBy: http.User.Identity?.Name);
    var stored = await configStore.SetAsync(entry, ct);
    return Results.Ok(stored);
})
.WithName("AdminSetConfiguration")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapDelete("/api/admin/config/{key}", async (
    string key,
    string? environment,
    IConfigurationStore configStore,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("DeleteConfiguration", key, http.User);
    var deleted = await configStore.DeleteAsync(key, environment ?? "default", ct);
    return deleted ? Results.NoContent() : Results.NotFound();
})
.WithName("AdminDeleteConfiguration")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Feature Flags ─────────────────────────────────────────────────────────────
// Dynamic feature flag management with rollout percentage and tenant targeting.

app.MapGet("/api/admin/features", async (
    IFeatureFlagService featureService,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("ListFeatureFlags", null, http.User);
    var flags = await featureService.ListAsync(ct);
    return Results.Ok(flags);
})
.WithName("AdminListFeatureFlags")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/features/{name}", async (
    string name,
    IFeatureFlagService featureService,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetFeatureFlag", name, http.User);
    var flag = await featureService.GetAsync(name, ct);
    return flag is null ? Results.NotFound() : Results.Ok(flag);
})
.WithName("AdminGetFeatureFlag")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapPut("/api/admin/features/{name}", async (
    string name,
    SetFeatureFlagRequest request,
    IFeatureFlagService featureService,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("SetFeatureFlag", name, http.User);
    var flag = new FeatureFlag(
        Name: name,
        IsEnabled: request.IsEnabled,
        Variants: request.Variants,
        RolloutPercentage: request.RolloutPercentage,
        TargetTenants: request.TargetTenants);
    await featureService.SetAsync(flag, ct);
    return Results.Ok(flag);
})
.WithName("AdminSetFeatureFlag")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapDelete("/api/admin/features/{name}", async (
    string name,
    IFeatureFlagService featureService,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("DeleteFeatureFlag", name, http.User);
    var deleted = await featureService.DeleteAsync(name, ct);
    return deleted ? Results.NoContent() : Results.NotFound();
})
.WithName("AdminDeleteFeatureFlag")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.Run();

// ── Request models ────────────────────────────────────────────────────────────

/// <summary>
/// Request body for updating the delivery status of a message.
/// </summary>
/// <param name="CorrelationId">Correlation identifier (partition key in Cassandra).</param>
/// <param name="RecordedAt">Original recorded-at timestamp (clustering key in Cassandra).</param>
/// <param name="Status">The new delivery status to apply.</param>
public sealed record UpdateMessageStatusRequest(
    Guid CorrelationId,
    DateTimeOffset RecordedAt,
    DeliveryStatus Status);

/// <summary>
/// Request body for resubmitting messages from the Dead Letter Queue.
/// All filter fields are optional; omitting all fields resubmits all DLQ messages up to the
/// platform's configured <c>Replay:MaxMessages</c> limit.
/// </summary>
/// <param name="CorrelationId">Optional correlation ID to filter which messages to resubmit.</param>
/// <param name="MessageType">Optional message type filter.</param>
/// <param name="FromTimestamp">Optional lower bound for the message timestamp range.</param>
/// <param name="ToTimestamp">Optional upper bound for the message timestamp range.</param>
public sealed record DlqResubmitRequest(
    Guid? CorrelationId,
    string? MessageType,
    DateTimeOffset? FromTimestamp,
    DateTimeOffset? ToTimestamp);

/// <summary>
/// Request body for creating or updating a throttle policy.
/// Throttle policies control message processing throughput per tenant, queue, or endpoint.
/// </summary>
/// <param name="PolicyId">Unique policy identifier.</param>
/// <param name="Name">Human-readable policy name.</param>
/// <param name="TenantId">Optional tenant/customer to scope the policy to.</param>
/// <param name="Queue">Optional queue/topic to scope the policy to.</param>
/// <param name="Endpoint">Optional endpoint/server to scope the policy to.</param>
/// <param name="MaxMessagesPerSecond">Token-bucket refill rate.</param>
/// <param name="BurstCapacity">Token-bucket capacity (burst allowance).</param>
/// <param name="MaxWaitTimeSeconds">Maximum seconds a message waits for a token.</param>
/// <param name="RejectOnBackpressure">When true, reject immediately instead of waiting.</param>
/// <param name="IsEnabled">Whether this policy is currently active.</param>
public sealed record SetThrottlePolicyRequest(
    string PolicyId,
    string Name,
    string? TenantId,
    string? Queue,
    string? Endpoint,
    int MaxMessagesPerSecond = 100,
    int BurstCapacity = 200,
    int MaxWaitTimeSeconds = 30,
    bool RejectOnBackpressure = false,
    bool IsEnabled = true);

/// <summary>
/// Request body for creating or updating a configuration entry.
/// </summary>
/// <param name="Value">The configuration value.</param>
/// <param name="Environment">Target environment (defaults to "default").</param>
public sealed record SetConfigurationRequest(
    string Value,
    string? Environment);

/// <summary>
/// Request body for creating or updating a feature flag.
/// </summary>
/// <param name="IsEnabled">Whether the feature is enabled.</param>
/// <param name="Variants">Named variants with their associated values.</param>
/// <param name="RolloutPercentage">Percentage of traffic (0–100) that should receive this feature.</param>
/// <param name="TargetTenants">Tenants that always receive this feature.</param>
public sealed record SetFeatureFlagRequest(
    bool IsEnabled = false,
    Dictionary<string, string>? Variants = null,
    int RolloutPercentage = 100,
    List<string>? TargetTenants = null);
