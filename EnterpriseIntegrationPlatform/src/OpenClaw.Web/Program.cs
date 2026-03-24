using EnterpriseIntegrationPlatform.AI.Ollama;
using EnterpriseIntegrationPlatform.AI.RagFlow;
using EnterpriseIntegrationPlatform.Observability;
using OpenClaw.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register Ollama AI service — base address comes from Aspire's environment
// variable (Ollama__BaseAddress) or config, with localhost fallback for local dev
var ollamaBaseAddress = builder.Configuration["Ollama:BaseAddress"]
                        ?? OllamaServiceExtensions.DefaultBaseAddress;
builder.Services.AddOllamaService(ollamaBaseAddress);

// Register platform observability — Loki URL is injected by Aspire (Loki__BaseAddress)
var lokiBaseAddress = builder.Configuration["Loki:BaseAddress"]
                      ?? "http://localhost:15100";
builder.Services.AddPlatformObservability(lokiBaseAddress);

// Register RagFlow RAG service — base address comes from Aspire's environment
// variable (RagFlow__BaseAddress) or config, with localhost fallback for local dev
builder.Services.AddRagFlowService(builder.Configuration);

// Seed demo data so "where is my message?" works out of the box
builder.Services.AddHostedService<DemoDataSeeder>();

var app = builder.Build();

app.MapDefaultEndpoints();

// ── API endpoints ─────────────────────────────────────────────────────────────

var api = app.MapGroup("/api/inspect");

api.MapGet("/business/{businessKey}", async (
    string businessKey,
    MessageStateInspector inspector,
    CancellationToken ct) =>
{
    var result = await inspector.WhereIsAsync(businessKey, ct);
    return Results.Ok(result);
})
.WithName("InspectByBusinessKey");

api.MapGet("/correlation/{correlationId:guid}", async (
    Guid correlationId,
    MessageStateInspector inspector,
    CancellationToken ct) =>
{
    var result = await inspector.WhereIsByCorrelationAsync(correlationId, ct);
    return Results.Ok(result);
})
.WithName("InspectByCorrelation");

api.MapPost("/ask", async (
    AskRequest request,
    MessageStateInspector inspector,
    CancellationToken ct) =>
{
    var result = await inspector.WhereIsAsync(request.Query, ct);
    return Results.Ok(result);
})
.WithName("Ask");

// ── Ollama health status endpoint ─────────────────────────────────────────────

app.MapGet("/api/health/ollama", async (IOllamaService ollama, CancellationToken ct) =>
{
    var healthy = await ollama.IsHealthyAsync(ct);
    return Results.Ok(new { available = healthy, service = "ollama" });
})
.WithName("OllamaHealth");

// ── RagFlow health status endpoint ────────────────────────────────────────────

app.MapGet("/api/health/ragflow", async (IRagFlowService ragFlow, CancellationToken ct) =>
{
    var healthy = await ragFlow.IsHealthyAsync(ct);
    return Results.Ok(new { available = healthy, service = "ragflow" });
})
.WithName("RagFlowHealth");

// ── RAG context retrieval endpoints ───────────────────────────────────────────
// Developers use their own preferred AI provider (Copilot, Codex, Claude Code)
// to generate code. These endpoints provide RAG context from the platform's
// knowledge base so that external AI tools can produce accurate, convention-
// compliant integrations.

var generate = app.MapGroup("/api/generate");

generate.MapPost("/integration", async (
    GenerateIntegrationRequest request,
    IRagFlowService ragFlow,
    CancellationToken ct) =>
{
    // Retrieve relevant context from the platform knowledge base via RagFlow.
    // The developer's own AI provider (Copilot, Codex, Claude Code) uses this
    // context to generate integration code — the platform does not generate
    // code itself.
    var context = await ragFlow.RetrieveAsync(request.Description, cancellationToken: ct);

    return Results.Ok(new GenerateIntegrationResponse(
        RetrievedContext: context,
        ContextFound: !string.IsNullOrEmpty(context)));
})
.WithName("GenerateIntegration");

generate.MapPost("/chat", async (
    GenerateChatRequest request,
    IRagFlowService ragFlow,
    CancellationToken ct) =>
{
    // Use RagFlow's chat completion — combines retrieval + generation in one call
    var response = await ragFlow.ChatAsync(request.Question, request.ConversationId, ct);

    return Results.Ok(new GenerateChatResponse(
        Answer: response.Answer,
        ConversationId: response.ConversationId,
        ReferenceCount: response.References.Count));
})
.WithName("GenerateChat");

generate.MapGet("/datasets", async (
    IRagFlowService ragFlow,
    CancellationToken ct) =>
{
    var datasets = await ragFlow.ListDatasetsAsync(ct);
    return Results.Ok(datasets);
})
.WithName("ListDatasets");

generate.MapPost("/connector", async (
    GenerateConnectorRequest request,
    IRagFlowService ragFlow,
    CancellationToken ct) =>
{
    // Build a structured connector generation prompt and retrieve platform context.
    // The developer's AI provider uses the returned context to generate
    // a production-ready connector implementing IHttpConnector / ISftpConnector / etc.
    var query = $"Generate a {request.ConnectorType} connector. " +
                $"Target: {request.TargetDescription}. " +
                $"Authentication: {request.AuthenticationType ?? "none"}. " +
                $"Existing platform patterns: {string.Join(", ", request.RelatedPatterns ?? [])}";

    var context = await ragFlow.RetrieveAsync(query, cancellationToken: ct);

    return Results.Ok(new GenerateConnectorResponse(
        ConnectorType: request.ConnectorType,
        RetrievedContext: context,
        ContextFound: !string.IsNullOrEmpty(context)));
})
.WithName("GenerateConnector");

generate.MapPost("/schema", async (
    GenerateSchemaRequest request,
    IRagFlowService ragFlow,
    CancellationToken ct) =>
{
    // Retrieve schema-related context from the platform knowledge base.
    var query = $"Message schema for {request.MessageType}. " +
                $"Format: {request.Format}. " +
                (request.ExamplePayload is not null
                    ? $"Example payload: {request.ExamplePayload}"
                    : string.Empty);

    var context = await ragFlow.RetrieveAsync(query, cancellationToken: ct);

    return Results.Ok(new GenerateSchemaResponse(
        MessageType: request.MessageType,
        RetrievedContext: context,
        ContextFound: !string.IsNullOrEmpty(context)));
})
.WithName("GenerateSchema");

// ── Serve the embedded HTML UI at root ────────────────────────────────────────

app.MapGet("/", () => Results.Content(OpenClawHtml.Page, "text/html"))
   .ExcludeFromDescription();

app.Run();

/// <summary>Request body for the /api/inspect/ask endpoint.</summary>
public sealed record AskRequest(string Query);

/// <summary>Request body for the /api/generate/integration endpoint.</summary>
/// <param name="Description">Natural-language description of the integration to retrieve context for.</param>
public sealed record GenerateIntegrationRequest(string Description);

/// <summary>Response from the /api/generate/integration endpoint.</summary>
/// <param name="RetrievedContext">Relevant platform context retrieved from the RagFlow knowledge base.</param>
/// <param name="ContextFound">Whether RagFlow returned any matching context.</param>
public sealed record GenerateIntegrationResponse(string RetrievedContext, bool ContextFound);

/// <summary>Request body for the /api/generate/chat endpoint.</summary>
/// <param name="Question">The question or generation request.</param>
/// <param name="ConversationId">Optional conversation ID for multi-turn follow-up.</param>
public sealed record GenerateChatRequest(string Question, string? ConversationId = null);

/// <summary>Response from the /api/generate/chat endpoint.</summary>
/// <param name="Answer">The AI-generated answer.</param>
/// <param name="ConversationId">Conversation ID for follow-up questions.</param>
/// <param name="ReferenceCount">Number of source references used.</param>
public sealed record GenerateChatResponse(string Answer, string? ConversationId, int ReferenceCount);

/// <summary>Request body for the /api/generate/connector endpoint.</summary>
/// <param name="ConnectorType">Connector type: "http", "sftp", "email", or "file".</param>
/// <param name="TargetDescription">Description of the target system (e.g. "Acme REST API v2").</param>
/// <param name="AuthenticationType">Optional authentication type (e.g. "OAuth2", "ApiKey", "Basic").</param>
/// <param name="RelatedPatterns">Optional list of EIP patterns the connector should integrate with.</param>
public sealed record GenerateConnectorRequest(
    string ConnectorType,
    string TargetDescription,
    string? AuthenticationType = null,
    IReadOnlyList<string>? RelatedPatterns = null);

/// <summary>Response from the /api/generate/connector endpoint.</summary>
/// <param name="ConnectorType">The requested connector type.</param>
/// <param name="RetrievedContext">Context retrieved from the platform knowledge base.</param>
/// <param name="ContextFound">Whether relevant context was found.</param>
public sealed record GenerateConnectorResponse(
    string ConnectorType,
    string RetrievedContext,
    bool ContextFound);

/// <summary>Request body for the /api/generate/schema endpoint.</summary>
/// <param name="MessageType">Logical message type name (e.g. "OrderCreated").</param>
/// <param name="Format">Payload format: "json", "xml", or "flat".</param>
/// <param name="ExamplePayload">Optional example payload to guide schema generation.</param>
public sealed record GenerateSchemaRequest(
    string MessageType,
    string Format,
    string? ExamplePayload = null);

/// <summary>Response from the /api/generate/schema endpoint.</summary>
/// <param name="MessageType">The requested message type.</param>
/// <param name="RetrievedContext">Context retrieved from the platform knowledge base.</param>
/// <param name="ContextFound">Whether relevant context was found.</param>
public sealed record GenerateSchemaResponse(
    string MessageType,
    string RetrievedContext,
    bool ContextFound);

/// <summary>
/// Contains the embedded HTML page for the OpenClaw web UI.
/// The page is responsive and works on any device (desktop, tablet, phone).
/// </summary>
internal static class OpenClawHtml
{
    internal const string Page = """
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <title>OpenClaw – Integration Observer</title>
        <style>
            :root {
                --bg: #0f172a; --surface: #1e293b; --border: #334155;
                --text: #e2e8f0; --muted: #94a3b8; --accent: #38bdf8;
                --green: #4ade80; --red: #f87171; --yellow: #facc15;
            }
            * { box-sizing: border-box; margin: 0; padding: 0; }
            body {
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                background: var(--bg); color: var(--text);
                min-height: 100vh; display: flex; flex-direction: column;
            }
            header {
                background: var(--surface); border-bottom: 1px solid var(--border);
                padding: 1rem 1.5rem; display: flex; align-items: center; gap: 0.75rem;
            }
            header h1 { font-size: 1.25rem; font-weight: 600; }
            header span { color: var(--accent); font-size: 1.5rem; }
            .ollama-status {
                margin-left: auto; font-size: 0.8rem; padding: 0.3rem 0.7rem;
                border-radius: 1rem; font-weight: 600;
            }
            .ollama-up { background: var(--green); color: #000; }
            .ollama-down { background: var(--red); color: #fff; }
            .ollama-checking { background: var(--muted); color: #000; }
            .container { max-width: 52rem; margin: 0 auto; padding: 1.5rem; width: 100%; flex: 1; }
            .search-box {
                display: flex; gap: 0.5rem; margin-bottom: 1.5rem;
            }
            .search-box input {
                flex: 1; padding: 0.75rem 1rem; border-radius: 0.5rem;
                border: 1px solid var(--border); background: var(--surface);
                color: var(--text); font-size: 1rem; outline: none;
            }
            .search-box input:focus { border-color: var(--accent); }
            .search-box input::placeholder { color: var(--muted); }
            .search-box button {
                padding: 0.75rem 1.5rem; border-radius: 0.5rem; border: none;
                background: var(--accent); color: var(--bg); font-weight: 600;
                cursor: pointer; font-size: 1rem; white-space: nowrap;
            }
            .search-box button:disabled { opacity: 0.5; cursor: not-allowed; }
            .hint { color: var(--muted); font-size: 0.85rem; margin-bottom: 1.5rem; }
            #result { display: none; }
            .card {
                background: var(--surface); border: 1px solid var(--border);
                border-radius: 0.75rem; padding: 1.25rem; margin-bottom: 1rem;
            }
            .card h2 { font-size: 1rem; margin-bottom: 0.75rem; color: var(--accent); }
            .summary { white-space: pre-wrap; line-height: 1.6; }
            .status-badge {
                display: inline-block; padding: 0.2rem 0.6rem; border-radius: 1rem;
                font-size: 0.75rem; font-weight: 600; text-transform: uppercase;
            }
            .status-Pending { background: var(--yellow); color: #000; }
            .status-InFlight { background: var(--accent); color: #000; }
            .status-Delivered { background: var(--green); color: #000; }
            .status-Failed, .status-DeadLettered { background: var(--red); color: #fff; }
            .status-Retrying { background: #fb923c; color: #000; }
            .timeline { list-style: none; position: relative; padding-left: 1.5rem; }
            .timeline::before {
                content: ''; position: absolute; left: 0.45rem; top: 0.5rem;
                bottom: 0.5rem; width: 2px; background: var(--border);
            }
            .timeline li {
                position: relative; margin-bottom: 1rem; padding-left: 1rem;
            }
            .timeline li::before {
                content: ''; position: absolute; left: -1.1rem; top: 0.35rem;
                width: 0.6rem; height: 0.6rem; border-radius: 50%;
                background: var(--accent); border: 2px solid var(--surface);
            }
            .timeline .time { color: var(--muted); font-size: 0.8rem; }
            .timeline .stage { font-weight: 600; }
            .not-found { text-align: center; padding: 2rem; color: var(--muted); }
            .spinner { display: none; text-align: center; padding: 2rem; color: var(--muted); }
            @media (max-width: 640px) {
                .container { padding: 1rem; }
                .search-box { flex-direction: column; }
                .search-box button { width: 100%; }
            }
        </style>
    </head>
    <body>
        <header>
            <span>🔍</span>
            <h1>OpenClaw – Integration Observer</h1>
            <span id="ollamaStatus" class="ollama-status ollama-checking">Ollama: checking…</span>
        </header>
        <div class="container">
            <div class="search-box">
                <input type="text" id="query" placeholder="e.g. order-02, shipment-123, or a correlation ID"
                       autofocus />
                <button id="askBtn" onclick="ask()">Ask</button>
            </div>
            <p class="hint">
                Ask where your message is by entering an order number, shipment ID,
                business key, or correlation ID. OpenClaw queries the isolated
                observability store (backed by Prometheus metrics + event log)
                and provides trace analysis to tell you exactly where it is.
                The platform's RAG API (powered by RagFlow + Ollama) provides
                knowledge retrieval — developers use their own AI provider
                (Copilot, Codex, Claude Code) for code generation.
            </p>
            <div class="spinner" id="spinner">⏳ Searching…</div>
            <div id="result"></div>
        </div>
        <script>
            const input = document.getElementById('query');
            input.addEventListener('keydown', e => { if (e.key === 'Enter') ask(); });

            // Check Ollama health on page load
            checkOllamaHealth();
            setInterval(checkOllamaHealth, 30000);

            async function checkOllamaHealth() {
                const el = document.getElementById('ollamaStatus');
                try {
                    const res = await fetch('/api/health/ollama');
                    const data = await res.json();
                    if (data.available) {
                        el.textContent = 'Ollama: connected';
                        el.className = 'ollama-status ollama-up';
                    } else {
                        el.textContent = '⚠️ Ollama: unavailable';
                        el.className = 'ollama-status ollama-down';
                    }
                } catch {
                    el.textContent = '⚠️ Ollama: unavailable';
                    el.className = 'ollama-status ollama-down';
                }
            }

            async function ask() {
                const q = input.value.trim();
                if (!q) return;
                const btn = document.getElementById('askBtn');
                const spinner = document.getElementById('spinner');
                const resultDiv = document.getElementById('result');
                btn.disabled = true;
                spinner.style.display = 'block';
                resultDiv.style.display = 'none';

                try {
                    // Detect if it's a GUID (correlation ID) or business key
                    const isGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(q);
                    const url = isGuid
                        ? `/api/inspect/correlation/${q}`
                        : `/api/inspect/business/${encodeURIComponent(q)}`;

                    const res = await fetch(url);
                    const data = await res.json();
                    renderResult(data);
                } catch (err) {
                    resultDiv.innerHTML = `<div class="card"><p>Error: ${err.message}</p></div>`;
                    resultDiv.style.display = 'block';
                } finally {
                    btn.disabled = false;
                    spinner.style.display = 'none';
                }
            }

            function renderResult(data) {
                const div = document.getElementById('result');
                if (!data.found) {
                    div.innerHTML = `<div class="not-found">
                        <p>😕 No messages found for <strong>${esc(data.query)}</strong></p>
                        <p style="margin-top:0.5rem">Try a different order number, shipment ID, or correlation ID.</p>
                    </div>`;
                    div.style.display = 'block';
                    return;
                }

                let html = '';

                // Ollama unavailable notification
                if (data.ollamaAvailable === false) {
                    html += `<div class="card" style="border-color:var(--yellow)">
                        <h2 style="color:var(--yellow)">⚠️ Ollama Unavailable</h2>
                        <div class="summary">${esc(data.summary)}</div>
                    </div>`;
                } else {
                    // AI Summary card
                    html += `<div class="card">
                        <h2>Trace Analysis Summary</h2>
                        <div class="summary">${esc(data.summary)}</div>
                    </div>`;
                }

                // Current status card
                if (data.latestStage) {
                    html += `<div class="card">
                        <h2>Current Status</h2>
                        <p>Stage: <strong>${esc(data.latestStage)}</strong>
                           <span class="status-badge status-${esc(data.latestStatus || '')}">${esc(data.latestStatus || 'Unknown')}</span>
                        </p>
                    </div>`;
                }

                // Timeline
                if (data.events && data.events.length > 0) {
                    html += `<div class="card"><h2>Lifecycle Timeline</h2><ul class="timeline">`;
                    for (const ev of data.events) {
                        const t = new Date(ev.recordedAt).toLocaleString();
                        html += `<li>
                            <span class="stage">${esc(ev.stage)}</span>
                            <span class="status-badge status-${esc(ev.status)}">${esc(ev.status)}</span>
                            <br/><span class="time">${t}</span>
                            ${ev.details ? '<br/><span style="color:var(--muted);font-size:0.85rem">' + esc(ev.details) + '</span>' : ''}
                        </li>`;
                    }
                    html += `</ul></div>`;
                }

                div.innerHTML = html;
                div.style.display = 'block';
            }

            function esc(s) {
                const d = document.createElement('div');
                d.textContent = s;
                return d.innerHTML;
            }
        </script>
    </body>
    </html>
    """;
}
