using System.Threading.RateLimiting;
using EnterpriseIntegrationPlatform.Admin.Api;
using EnterpriseIntegrationPlatform.Admin.Api.Authentication;
using EnterpriseIntegrationPlatform.Admin.Api.Services;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using EnterpriseIntegrationPlatform.Processing.Replay;
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
