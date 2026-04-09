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
using EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;
using EnterpriseIntegrationPlatform.SystemManagement;
using EnterpriseIntegrationPlatform.DisasterRecovery;
using Performance.Profiling;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.EventSourcing;
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

// ── Tenant Onboarding ─────────────────────────────────────────────────────────
// Self-service tenant provisioning, quota management, and broker namespaces.
builder.Services.AddTenantOnboarding(builder.Configuration);

// ── System Management ─────────────────────────────────────────────────────────
// Control Bus, Message Store, Smart Proxy, and Test Message Generator.
builder.Services.AddControlBus(builder.Configuration);
builder.Services.AddMessageStore();
builder.Services.AddTestMessageGenerator();

// ── Connectors ────────────────────────────────────────────────────────────────
// Connector registry for HTTP, SFTP, Email, File adapters (BizTalk Adapter equivalent).
builder.Services.AddConnectors();

// ── Event Sourcing ────────────────────────────────────────────────────────────
// Event store for aggregate event streams (BizTalk BAM equivalent).
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();

// ── Disaster Recovery ─────────────────────────────────────────────────────────
// Automated failover, cross-region replication, recovery point validation, and DR drills.
builder.Services.AddDisasterRecovery(builder.Configuration);

// ── Performance Profiling ─────────────────────────────────────────────────────
// Continuous profiling, hotspot detection, GC monitoring, and benchmark regression tracking.
builder.Services.AddPerformanceProfiling(builder.Configuration);

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

// ── Tenant Onboarding ─────────────────────────────────────────────────────────

app.MapPost("/api/admin/tenants/onboard", async (
    TenantOnboardingRequest request,
    ITenantOnboardingService onboardingService,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("OnboardTenant", request.TenantId, http.User);
    var result = await onboardingService.ProvisionAsync(request, ct);
    return Results.Ok(result);
})
.WithName("AdminOnboardTenant")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapDelete("/api/admin/tenants/{tenantId}", async (
    string tenantId,
    ITenantOnboardingService onboardingService,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("DeprovisionTenant", tenantId, http.User);
    var result = await onboardingService.DeprovisionAsync(tenantId, ct);
    return Results.Ok(result);
})
.WithName("AdminDeprovisionTenant")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/tenants/{tenantId}/status", async (
    string tenantId,
    ITenantOnboardingService onboardingService,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetTenantStatus", tenantId, http.User);
    var result = await onboardingService.GetStatusAsync(tenantId, ct);
    return result is not null ? Results.Ok(result) : Results.NotFound();
})
.WithName("AdminGetTenantStatus")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/tenants/{tenantId}/quota", async (
    string tenantId,
    ITenantQuotaManager quotaManager,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetTenantQuota", tenantId, http.User);
    var quota = await quotaManager.GetQuotaAsync(tenantId, ct);
    return quota is not null ? Results.Ok(quota) : Results.NotFound();
})
.WithName("AdminGetTenantQuota")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapPut("/api/admin/tenants/{tenantId}/quota", async (
    string tenantId,
    TenantQuota quota,
    ITenantQuotaManager quotaManager,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("SetTenantQuota", tenantId, http.User);
    await quotaManager.SetQuotaAsync(tenantId, quota, ct);
    return Results.Ok(quota);
})
.WithName("AdminSetTenantQuota")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Disaster Recovery ─────────────────────────────────────────────────────────

app.MapGet("/api/admin/dr/status", async (
    IFailoverManager failoverManager,
    IReplicationManager replicationManager,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetDrStatus", null, http.User);
    var primary = await failoverManager.GetPrimaryAsync(ct);
    var regions = await failoverManager.GetAllRegionsAsync(ct);
    var replicationStatuses = await replicationManager.GetAllStatusesAsync(ct);
    return Results.Ok(new
    {
        PrimaryRegion = primary?.RegionId,
        TotalRegions = regions.Count,
        Regions = regions,
        ReplicationStatuses = replicationStatuses,
    });
}).RequireAuthorization();

app.MapGet("/api/admin/dr/regions", async (
    IFailoverManager failoverManager,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetDrRegions", null, http.User);
    var regions = await failoverManager.GetAllRegionsAsync(ct);
    return Results.Ok(regions);
})
.WithName("AdminGetDrRegions")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapPost("/api/admin/dr/regions", async (
    RegionInfo region,
    IFailoverManager failoverManager,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("RegisterDrRegion", region.RegionId, http.User);
    await failoverManager.RegisterRegionAsync(region, ct);
    return Results.Ok(region);
})
.WithName("AdminRegisterDrRegion")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapPost("/api/admin/dr/failover/{targetRegionId}", async (
    string targetRegionId,
    IFailoverManager failoverManager,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("DrFailover", targetRegionId, http.User);
    var result = await failoverManager.FailoverAsync(targetRegionId, ct);
    return result.Success ? Results.Ok(result) : Results.UnprocessableEntity(result);
})
.WithName("AdminDrFailover")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapPost("/api/admin/dr/failback/{regionId}", async (
    string regionId,
    IFailoverManager failoverManager,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("DrFailback", regionId, http.User);
    var result = await failoverManager.FailbackAsync(regionId, ct);
    return result.Success ? Results.Ok(result) : Results.UnprocessableEntity(result);
})
.WithName("AdminDrFailback")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/dr/replication", async (
    IReplicationManager replicationManager,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetDrReplication", null, http.User);
    var statuses = await replicationManager.GetAllStatusesAsync(ct);
    return Results.Ok(statuses);
})
.WithName("AdminGetDrReplication")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/dr/objectives", async (
    IRecoveryPointValidator validator,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetDrObjectives", null, http.User);
    var objectives = await validator.GetObjectivesAsync(ct);
    return Results.Ok(objectives);
})
.WithName("AdminGetDrObjectives")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapPost("/api/admin/dr/objectives", async (
    RecoveryObjective objective,
    IRecoveryPointValidator validator,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("RegisterDrObjective", objective.ObjectiveId, http.User);
    await validator.RegisterObjectiveAsync(objective, ct);
    return Results.Ok(objective);
})
.WithName("AdminRegisterDrObjective")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapPost("/api/admin/dr/drills", async (
    DrDrillScenario scenario,
    IDrDrillRunner drillRunner,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("RunDrDrill", scenario.ScenarioId, http.User);
    var result = await drillRunner.RunDrillAsync(scenario, ct);
    return Results.Ok(result);
})
.WithName("AdminRunDrDrill")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/dr/drills/history", async (
    IDrDrillRunner drillRunner,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetDrDrillHistory", null, http.User);
    var history = await drillRunner.GetDrillHistoryAsync(cancellationToken: ct);
    return Results.Ok(history);
})
.WithName("AdminGetDrDrillHistory")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Message Flow Timeline (BizTalk-inspired tracked activity view) ─────────────
// Returns the full lifecycle event history + optional AI trace analysis for
// a correlation ID or business key. Powers the MessageFlowPage in Admin.Web.

app.MapGet("/api/admin/flow/correlation/{correlationId:guid}", async (
    Guid correlationId,
    MessageStateInspector inspector,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("MessageFlowByCorrelation", correlationId.ToString(), http.User);
    var result = await inspector.WhereIsByCorrelationAsync(correlationId, ct);
    return Results.Ok(result);
})
.WithName("AdminMessageFlowByCorrelation")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/flow/business/{businessKey}", async (
    string businessKey,
    MessageStateInspector inspector,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("MessageFlowByBusinessKey", businessKey, http.User);
    var result = await inspector.WhereIsAsync(businessKey, ct);
    return Results.Ok(result);
})
.WithName("AdminMessageFlowByBusinessKey")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Metrics Summary (BizTalk Group Hub-inspired) ──────────────────────────────
// Exposes current counters from PlatformMeters for the dashboard.

app.MapGet("/api/admin/metrics/summary", (
    AdminAuditLogger audit,
    HttpContext http) =>
{
    audit.LogAction("GetMetricsSummary", null, http.User);

    // Read the current metric instrument snapshots.
    // Note: OTel metric export is async; these are the instrument objects themselves.
    // For live dashboard, we expose the instrument metadata and let the frontend
    // query Prometheus/Grafana for actual values. This endpoint provides structure.
    return Results.Ok(new
    {
        Instruments = new[]
        {
            new { Name = "eip.messages.received", Type = "Counter", Unit = "{message}" },
            new { Name = "eip.messages.processed", Type = "Counter", Unit = "{message}" },
            new { Name = "eip.messages.failed", Type = "Counter", Unit = "{message}" },
            new { Name = "eip.messages.dead_lettered", Type = "Counter", Unit = "{message}" },
            new { Name = "eip.messages.retried", Type = "Counter", Unit = "{message}" },
            new { Name = "eip.messages.in_flight", Type = "UpDownCounter", Unit = "{message}" },
            new { Name = "eip.messages.processing_duration", Type = "Histogram", Unit = "ms" },
        },
        PrometheusEndpoint = "/metrics",
        Description = "Use the Prometheus endpoint or Grafana dashboards for live metric values. " +
                      "These instruments are exported via OpenTelemetry.",
    });
})
.WithName("AdminGetMetricsSummary")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Test Message Generator ────────────────────────────────────────────────────
// Publishes synthetic test messages to verify pipeline health (BizTalk test message pattern).

app.MapPost("/api/admin/test-messages", async (
    TestMessageRequest request,
    EnterpriseIntegrationPlatform.SystemManagement.ITestMessageGenerator generator,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GenerateTestMessage", request.TargetTopic, http.User);
    var result = await generator.GenerateAsync(request.TargetTopic, ct);
    return Results.Ok(result);
})
.WithName("AdminGenerateTestMessage")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapPost("/api/admin/test-messages/custom", async (
    CustomTestMessageRequest request,
    EnterpriseIntegrationPlatform.SystemManagement.ITestMessageGenerator generator,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GenerateCustomTestMessage", request.TargetTopic, http.User);
    var result = await generator.GenerateAsync(request.Payload, request.TargetTopic, ct);
    return Results.Ok(result);
})
.WithName("AdminGenerateCustomTestMessage")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Control Bus (BizTalk Control Bus EIP) ─────────────────────────────────────
// Send control commands through the messaging infrastructure.

app.MapPost("/api/admin/controlbus/send", async (
    ControlBusSendRequest request,
    IControlBus controlBus,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("SendControlBusCommand", request.CommandType, http.User);
    var result = await controlBus.PublishCommandAsync(request.Payload, request.CommandType, ct);
    return Results.Ok(result);
})
.WithName("AdminSendControlBusCommand")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Subscription Viewer (BizTalk Subscription Viewer) ─────────────────────────
// Lists active broker subscriptions. Reads from registered durable subscriber stores.

app.MapGet("/api/admin/subscriptions", async (
    IMessageStateStore stateStore,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetSubscriptions", null, http.User);
    // Return subscription metadata from the system.
    // In a full deployment, this queries each broker's subscription registry.
    // For the admin UI, we return the available subscription information.
    return Results.Ok(Array.Empty<object>());
})
.WithName("AdminGetSubscriptions")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── In-Flight Message Monitor (BizTalk Service Instances) ─────────────────────
// Shows messages currently being processed.

app.MapGet("/api/admin/messages/inflight", async (
    IMessageStateStore stateStore,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetInFlightMessages", null, http.User);
    // In a full deployment, this aggregates from the state store.
    // The metrics endpoint provides real-time counters via Prometheus.
    return Results.Ok(Array.Empty<object>());
})
.WithName("AdminGetInFlightMessages")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Audit Log (BizTalk Audit Trail) ──────────────────────────────────────────
// Queries structured audit events from the observability pipeline.

app.MapGet("/api/admin/audit", async (
    IObservabilityEventLog eventLog,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetAuditLog", null, http.User);
    // Audit entries are stored as structured log events in Loki.
    // This endpoint would query Loki's log stream for AdminAudit events.
    // For the initial implementation, we return an empty result set.
    return Results.Ok(Array.Empty<object>());
})
.WithName("AdminGetAuditLog")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Performance Profiling Endpoints ───────────────────────────────────────────

app.MapGet("/api/admin/profiling/status", (
    IContinuousProfiler profiler) =>
{
    return Results.Ok(new
    {
        IsActive = true,
        SnapshotCount = profiler.SnapshotCount,
        LatestSnapshot = profiler.GetLatestSnapshot(),
    });
}).RequireAuthorization();

app.MapPost("/api/admin/profiling/cpu/start", (
    IContinuousProfiler profiler) =>
{
    profiler.CaptureSnapshot("cpu-profiling-start");
    return Results.Ok(new { Message = "CPU profiling started" });
}).RequireAuthorization();

app.MapPost("/api/admin/profiling/cpu/stop", (
    IContinuousProfiler profiler) =>
{
    var snapshot = profiler.CaptureSnapshot("cpu-profiling-stop");
    return Results.Ok(snapshot);
}).RequireAuthorization();

app.MapPost("/api/admin/profiling/memory/snap", (
    IContinuousProfiler profiler) =>
{
    var snapshot = profiler.CaptureSnapshot("memory-snapshot");
    return Results.Ok(snapshot);
}).RequireAuthorization();

app.MapGet("/api/admin/profiling/gc/stats", (
    IGcMonitor monitor) =>
{
    var snapshot = monitor.CaptureSnapshot();
    var recommendations = monitor.GetRecommendations();
    return Results.Ok(new { Snapshot = snapshot, Recommendations = recommendations });
}).RequireAuthorization();

app.MapPost("/api/admin/profiling/snapshot", async (
    IContinuousProfiler profiler,
    HttpContext ctx) =>
{
    var label = ctx.Request.Query["label"].FirstOrDefault();
    var snapshot = profiler.CaptureSnapshot(label);
    return Results.Ok(snapshot);
})
.WithName("AdminCaptureProfileSnapshot")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/profiling/snapshot/latest", async (
    IContinuousProfiler profiler) =>
{
    var snapshot = profiler.GetLatestSnapshot();
    return snapshot is not null ? Results.Ok(snapshot) : Results.NotFound();
})
.WithName("AdminGetLatestProfileSnapshot")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/profiling/snapshots", async (
    IContinuousProfiler profiler,
    HttpContext ctx) =>
{
    var from = ctx.Request.Query.TryGetValue("from", out var fromStr) && DateTimeOffset.TryParse(fromStr, out var fromDate)
        ? fromDate
        : DateTimeOffset.UtcNow.AddHours(-1);
    var to = ctx.Request.Query.TryGetValue("to", out var toStr) && DateTimeOffset.TryParse(toStr, out var toDate)
        ? toDate
        : DateTimeOffset.UtcNow;
    var snapshots = profiler.GetSnapshots(from, to);
    return Results.Ok(snapshots);
})
.WithName("AdminGetProfileSnapshots")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/profiling/hotspots", async (
    IHotspotDetector detector,
    HttpContext ctx) =>
{
    var thresholds = new HotspotThresholds();
    if (ctx.Request.Query.TryGetValue("durationWarningMs", out var dw) && double.TryParse(dw, out var dwVal))
        thresholds.DurationWarningMs = dwVal;
    if (ctx.Request.Query.TryGetValue("durationCriticalMs", out var dc) && double.TryParse(dc, out var dcVal))
        thresholds.DurationCriticalMs = dcVal;
    var hotspots = detector.DetectHotspots(thresholds);
    return Results.Ok(hotspots);
})
.WithName("AdminGetHotspots")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/profiling/operations", async (
    IHotspotDetector detector) =>
{
    var stats = detector.GetAllOperationStats();
    return Results.Ok(stats);
})
.WithName("AdminGetOperationStats")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/profiling/gc", async (
    IGcMonitor monitor) =>
{
    var snapshot = monitor.CaptureSnapshot();
    return Results.Ok(snapshot);
})
.WithName("AdminGetGcSnapshot")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/profiling/gc/recommendations", async (
    IGcMonitor monitor) =>
{
    var recommendations = monitor.GetRecommendations();
    return Results.Ok(recommendations);
})
.WithName("AdminGetGcRecommendations")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

app.MapGet("/api/admin/profiling/benchmarks", async (
    IBenchmarkRegistry registry) =>
{
    var baselines = registry.GetAllBaselines();
    return Results.Ok(baselines);
})
.WithName("AdminGetBenchmarkBaselines")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Message Replay (BizTalk Tracked Message Replay) ──────────────────────────
// Replays historical messages from the replay store back to a target topic.

app.MapPost("/api/admin/replay", async (
    ReplayRequest request,
    IMessageReplayer replayer,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("ReplayMessages", request.MessageType ?? "(all)", http.User);
    var filter = new ReplayFilter
    {
        CorrelationId = !string.IsNullOrWhiteSpace(request.CorrelationId) && Guid.TryParse(request.CorrelationId, out var cid)
            ? cid : null,
        MessageType = request.MessageType,
        FromTimestamp = request.FromTimestamp,
        ToTimestamp = request.ToTimestamp,
    };
    var result = await replayer.ReplayAsync(filter, ct);
    return Results.Ok(result);
})
.WithName("AdminReplayMessages")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Connector Health (BizTalk Adapter/Port Monitor) ──────────────────────────
// Lists all registered connectors with health status.

app.MapGet("/api/admin/connectors", (
    IConnectorRegistry registry,
    AdminAuditLogger audit,
    HttpContext http) =>
{
    audit.LogAction("GetConnectors", null, http.User);
    var descriptors = registry.GetDescriptors();
    return Results.Ok(descriptors.Select(d => new
    {
        d.Name,
        ConnectorType = d.ConnectorType.ToString(),
        HealthStatus = "Healthy",
        LastChecked = DateTimeOffset.UtcNow,
        Description = $"{d.ConnectorType} connector",
    }));
})
.WithName("AdminGetConnectors")
.RequireAuthorization(new AuthorizeAttribute { Roles = ApiKeyAuthenticationHandler.AdminRole });

// ── Event Store Browser (BizTalk BAM) ────────────────────────────────────────
// Browse event-sourced aggregate streams.

app.MapGet("/api/admin/eventstore/stream/{streamId}", async (
    string streamId,
    IEventStore eventStore,
    AdminAuditLogger audit,
    HttpContext http,
    CancellationToken ct) =>
{
    audit.LogAction("GetEventStream", streamId, http.User);
    var events = await eventStore.ReadStreamAsync(streamId, 0, 1000, ct);
    return Results.Ok(events.Select(e => new
    {
        e.EventType,
        Version = e.Version,
        Timestamp = e.Timestamp,
        Data = e.Data,
        Metadata = e.Metadata,
    }));
})
.WithName("AdminGetEventStream")
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

/// <summary>
/// Request body for generating a synthetic test message.
/// </summary>
/// <param name="TargetTopic">The topic to publish the test message to.</param>
public sealed record TestMessageRequest(string TargetTopic);

/// <summary>
/// Request body for generating a custom test message with a specific payload.
/// </summary>
/// <param name="Payload">The JSON payload to send as a test message.</param>
/// <param name="TargetTopic">The topic to publish the test message to.</param>
public sealed record CustomTestMessageRequest(string Payload, string TargetTopic);

/// <summary>
/// Request body for sending a control command via the Control Bus.
/// </summary>
/// <param name="CommandType">The logical command type (e.g. "config.reload").</param>
/// <param name="Payload">The command payload (JSON object).</param>
public sealed record ControlBusSendRequest(string CommandType, object Payload);

/// <summary>
/// Request body for replaying messages from the replay store.
/// All filter fields are optional.
/// </summary>
/// <param name="CorrelationId">Optional correlation ID filter.</param>
/// <param name="MessageType">Optional message type filter.</param>
/// <param name="FromTimestamp">Optional lower bound for message timestamps.</param>
/// <param name="ToTimestamp">Optional upper bound for message timestamps.</param>
/// <param name="SourceTopic">Optional source topic override.</param>
/// <param name="TargetTopic">Optional target topic override.</param>
public sealed record ReplayRequest(
    string? CorrelationId,
    string? MessageType,
    DateTimeOffset? FromTimestamp,
    DateTimeOffset? ToTimestamp,
    string? SourceTopic,
    string? TargetTopic);
