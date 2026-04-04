namespace AdminWeb;

/// <summary>
/// Contains the embedded HTML page for the Admin Dashboard Vue 3 SPA.
/// The page provides a comprehensive administration interface for the
/// Enterprise Integration Platform, including throttle control, DLQ management,
/// DR drill execution, message inspection, and performance profiling.
/// </summary>
internal static class AdminDashboardHtml
{
    internal const string Page = """
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <title>EIP Admin Dashboard</title>
        <style>
            :root {
                --bg: #0f172a; --surface: #1e293b; --surface2: #283548;
                --border: #334155; --text: #e2e8f0; --muted: #94a3b8;
                --accent: #38bdf8; --green: #4ade80; --red: #f87171;
                --yellow: #facc15; --orange: #fb923c; --purple: #a78bfa;
            }
            * { box-sizing: border-box; margin: 0; padding: 0; }
            body {
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                background: var(--bg); color: var(--text);
                min-height: 100vh; display: flex;
            }
            /* Sidebar */
            .sidebar {
                width: 220px; background: var(--surface); border-right: 1px solid var(--border);
                padding: 1rem 0; display: flex; flex-direction: column; flex-shrink: 0;
                min-height: 100vh;
            }
            .sidebar h1 {
                font-size: 1rem; padding: 0 1rem 1rem; border-bottom: 1px solid var(--border);
                color: var(--accent); display: flex; align-items: center; gap: 0.5rem;
            }
            .sidebar nav { padding: 0.5rem 0; flex: 1; }
            .sidebar nav a {
                display: block; padding: 0.6rem 1rem; color: var(--muted);
                text-decoration: none; font-size: 0.9rem; cursor: pointer;
                border-left: 3px solid transparent; transition: all 0.15s;
            }
            .sidebar nav a:hover { color: var(--text); background: var(--surface2); }
            .sidebar nav a.active {
                color: var(--accent); border-left-color: var(--accent);
                background: var(--surface2); font-weight: 600;
            }
            /* Main content */
            .main { flex: 1; overflow-y: auto; max-height: 100vh; }
            .main header {
                background: var(--surface); border-bottom: 1px solid var(--border);
                padding: 1rem 1.5rem; display: flex; align-items: center; gap: 0.75rem;
            }
            .main header h2 { font-size: 1.1rem; font-weight: 600; }
            .content { padding: 1.5rem; max-width: 72rem; }
            /* Cards */
            .card {
                background: var(--surface); border: 1px solid var(--border);
                border-radius: 0.75rem; padding: 1.25rem; margin-bottom: 1rem;
            }
            .card h3 { font-size: 0.95rem; margin-bottom: 0.75rem; color: var(--accent); }
            .card-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 1rem; margin-bottom: 1rem; }
            .stat-card {
                background: var(--surface); border: 1px solid var(--border);
                border-radius: 0.75rem; padding: 1rem; text-align: center;
            }
            .stat-card .label { font-size: 0.8rem; color: var(--muted); margin-bottom: 0.25rem; }
            .stat-card .value { font-size: 1.5rem; font-weight: 700; }
            .stat-card .value.healthy { color: var(--green); }
            .stat-card .value.degraded { color: var(--yellow); }
            .stat-card .value.unhealthy { color: var(--red); }
            /* Table */
            table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
            th, td { padding: 0.6rem 0.75rem; text-align: left; border-bottom: 1px solid var(--border); }
            th { color: var(--muted); font-weight: 600; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; }
            /* Buttons */
            .btn {
                padding: 0.5rem 1rem; border-radius: 0.4rem; border: none;
                font-size: 0.85rem; font-weight: 600; cursor: pointer; transition: opacity 0.15s;
            }
            .btn:disabled { opacity: 0.5; cursor: not-allowed; }
            .btn-primary { background: var(--accent); color: var(--bg); }
            .btn-danger { background: var(--red); color: #fff; }
            .btn-success { background: var(--green); color: var(--bg); }
            .btn-warning { background: var(--orange); color: var(--bg); }
            .btn-sm { padding: 0.3rem 0.6rem; font-size: 0.8rem; }
            /* Form */
            .form-group { margin-bottom: 0.75rem; }
            .form-group label { display: block; font-size: 0.8rem; color: var(--muted); margin-bottom: 0.25rem; }
            .form-group input, .form-group select, .form-group textarea {
                width: 100%; padding: 0.5rem 0.75rem; border-radius: 0.4rem;
                border: 1px solid var(--border); background: var(--surface2);
                color: var(--text); font-size: 0.85rem; outline: none;
            }
            .form-group input:focus, .form-group select:focus { border-color: var(--accent); }
            .form-row { display: flex; gap: 0.75rem; }
            .form-row .form-group { flex: 1; }
            /* Badge */
            .badge {
                display: inline-block; padding: 0.15rem 0.5rem; border-radius: 1rem;
                font-size: 0.7rem; font-weight: 600; text-transform: uppercase;
            }
            .badge-healthy { background: var(--green); color: #000; }
            .badge-degraded { background: var(--yellow); color: #000; }
            .badge-unhealthy { background: var(--red); color: #fff; }
            .badge-enabled { background: var(--green); color: #000; }
            .badge-disabled { background: var(--muted); color: #000; }
            .badge-pass { background: var(--green); color: #000; }
            .badge-fail { background: var(--red); color: #fff; }
            /* Status */
            .status-indicator {
                display: inline-block; width: 8px; height: 8px; border-radius: 50%;
                margin-right: 0.4rem;
            }
            .status-green { background: var(--green); }
            .status-yellow { background: var(--yellow); }
            .status-red { background: var(--red); }
            /* Alert */
            .alert {
                padding: 0.75rem 1rem; border-radius: 0.5rem; margin-bottom: 1rem;
                font-size: 0.85rem;
            }
            .alert-info { background: rgba(56,189,248,0.15); border: 1px solid var(--accent); }
            .alert-success { background: rgba(74,222,128,0.15); border: 1px solid var(--green); }
            .alert-error { background: rgba(248,113,113,0.15); border: 1px solid var(--red); }
            .alert-warning { background: rgba(251,146,60,0.15); border: 1px solid var(--orange); }
            /* JSON block */
            .json-block {
                background: var(--bg); border: 1px solid var(--border); border-radius: 0.5rem;
                padding: 0.75rem; font-family: 'Fira Code', monospace; font-size: 0.8rem;
                overflow-x: auto; white-space: pre-wrap; max-height: 300px; overflow-y: auto;
            }
            .loading { text-align: center; padding: 2rem; color: var(--muted); }
            @media (max-width: 768px) {
                body { flex-direction: column; }
                .sidebar { width: 100%; min-height: auto; flex-direction: row; overflow-x: auto; }
                .sidebar h1 { display: none; }
                .sidebar nav { display: flex; padding: 0; }
                .sidebar nav a { border-left: none; border-bottom: 3px solid transparent; white-space: nowrap; }
                .sidebar nav a.active { border-bottom-color: var(--accent); }
            }
        </style>
    </head>
    <body>
        <div id="app">
            <div class="sidebar">
                <h1>⚙️ EIP Admin</h1>
                <nav>
                    <a :class="{ active: currentPage === 'dashboard' }" @click="currentPage = 'dashboard'" data-nav="dashboard">📊 Dashboard</a>
                    <a :class="{ active: currentPage === 'throttle' }" @click="currentPage = 'throttle'" data-nav="throttle">🔧 Throttle</a>
                    <a :class="{ active: currentPage === 'ratelimit' }" @click="currentPage = 'ratelimit'" data-nav="ratelimit">🚦 Rate Limiting</a>
                    <a :class="{ active: currentPage === 'dlq' }" @click="currentPage = 'dlq'" data-nav="dlq">📬 DLQ</a>
                    <a :class="{ active: currentPage === 'messages' }" @click="currentPage = 'messages'" data-nav="messages">🔍 Messages</a>
                    <a :class="{ active: currentPage === 'dr' }" @click="currentPage = 'dr'" data-nav="dr">🛡️ DR Drills</a>
                    <a :class="{ active: currentPage === 'profiling' }" @click="currentPage = 'profiling'" data-nav="profiling">📈 Profiling</a>
                </nav>
            </div>
            <div class="main">
                <header>
                    <h2>{{ pageTitle }}</h2>
                </header>
                <div class="content">
                    <!-- Dashboard -->
                    <div v-if="currentPage === 'dashboard'" id="page-dashboard">
                        <div v-if="statusLoading" class="loading">⏳ Loading platform status…</div>
                        <div v-else>
                            <div class="card-grid">
                                <div class="stat-card">
                                    <div class="label">Overall Health</div>
                                    <div class="value" :class="statusOverallClass">{{ status.overall || '—' }}</div>
                                </div>
                                <div class="stat-card">
                                    <div class="label">Components</div>
                                    <div class="value">{{ status.components ? status.components.length : 0 }}</div>
                                </div>
                                <div class="stat-card">
                                    <div class="label">Check Duration</div>
                                    <div class="value">{{ formatDuration(status.totalDuration) }}</div>
                                </div>
                                <div class="stat-card">
                                    <div class="label">Checked At</div>
                                    <div class="value" style="font-size:0.9rem">{{ formatDate(status.checkedAt) }}</div>
                                </div>
                            </div>
                            <div class="card" v-if="status.components && status.components.length">
                                <h3>Component Health</h3>
                                <table id="component-table">
                                    <thead><tr><th>Component</th><th>Status</th><th>Duration</th><th>Description</th></tr></thead>
                                    <tbody>
                                        <tr v-for="c in status.components" :key="c.name">
                                            <td>{{ c.name }}</td>
                                            <td><span class="badge" :class="'badge-' + c.status.toLowerCase()">{{ c.status }}</span></td>
                                            <td>{{ formatDuration(c.duration) }}</td>
                                            <td>{{ c.description || '—' }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                            <div v-if="statusError" class="alert alert-warning">{{ statusError }}</div>
                        </div>
                    </div>

                    <!-- Throttle Policies -->
                    <div v-if="currentPage === 'throttle'" id="page-throttle">
                        <div class="card">
                            <h3>Throttle Policies</h3>
                            <div style="margin-bottom:0.75rem">
                                <button class="btn btn-primary btn-sm" @click="showThrottleForm = !showThrottleForm" id="btn-add-throttle">
                                    {{ showThrottleForm ? '✕ Cancel' : '＋ Add Policy' }}
                                </button>
                                <button class="btn btn-sm" style="background:var(--surface2);color:var(--text);margin-left:0.5rem" @click="loadThrottlePolicies">↻ Refresh</button>
                            </div>
                            <div v-if="showThrottleForm" class="card" style="background:var(--surface2)" id="throttle-form">
                                <h3>{{ throttleForm.policyId ? 'Edit Policy' : 'New Policy' }}</h3>
                                <div class="form-row">
                                    <div class="form-group"><label>Policy ID</label><input v-model="throttleForm.policyId" id="throttle-policyId" /></div>
                                    <div class="form-group"><label>Name</label><input v-model="throttleForm.name" id="throttle-name" /></div>
                                </div>
                                <div class="form-row">
                                    <div class="form-group"><label>Tenant ID</label><input v-model="throttleForm.tenantId" id="throttle-tenantId" /></div>
                                    <div class="form-group"><label>Queue</label><input v-model="throttleForm.queue" id="throttle-queue" /></div>
                                    <div class="form-group"><label>Endpoint</label><input v-model="throttleForm.endpoint" id="throttle-endpoint" /></div>
                                </div>
                                <div class="form-row">
                                    <div class="form-group"><label>Max Msg/sec</label><input type="number" v-model.number="throttleForm.maxMessagesPerSecond" id="throttle-maxMps" /></div>
                                    <div class="form-group"><label>Burst Capacity</label><input type="number" v-model.number="throttleForm.burstCapacity" id="throttle-burst" /></div>
                                    <div class="form-group"><label>Max Wait (sec)</label><input type="number" v-model.number="throttleForm.maxWaitTimeSeconds" /></div>
                                </div>
                                <div class="form-row">
                                    <div class="form-group">
                                        <label><input type="checkbox" v-model="throttleForm.isEnabled" /> Enabled</label>
                                    </div>
                                    <div class="form-group">
                                        <label><input type="checkbox" v-model="throttleForm.rejectOnBackpressure" /> Reject on Backpressure</label>
                                    </div>
                                </div>
                                <button class="btn btn-success btn-sm" @click="saveThrottlePolicy" id="btn-save-throttle">💾 Save</button>
                            </div>
                            <div v-if="throttleLoading" class="loading">⏳ Loading…</div>
                            <table v-else id="throttle-table">
                                <thead><tr><th>Policy ID</th><th>Name</th><th>Tenant</th><th>Queue</th><th>Max Msg/s</th><th>Burst</th><th>Status</th><th>Actions</th></tr></thead>
                                <tbody>
                                    <tr v-for="p in throttlePolicies" :key="p.policyId">
                                        <td>{{ p.policyId }}</td>
                                        <td>{{ p.name }}</td>
                                        <td>{{ p.partition?.tenantId || '—' }}</td>
                                        <td>{{ p.partition?.queue || '—' }}</td>
                                        <td>{{ p.maxMessagesPerSecond }}</td>
                                        <td>{{ p.burstCapacity }}</td>
                                        <td><span class="badge" :class="p.isEnabled ? 'badge-enabled' : 'badge-disabled'">{{ p.isEnabled ? 'Enabled' : 'Disabled' }}</span></td>
                                        <td>
                                            <button class="btn btn-sm btn-warning" @click="editThrottlePolicy(p)">✏️</button>
                                            <button class="btn btn-sm btn-danger" @click="deleteThrottlePolicy(p.policyId)">🗑️</button>
                                        </td>
                                    </tr>
                                    <tr v-if="!throttlePolicies.length"><td colspan="8" style="text-align:center;color:var(--muted)">No throttle policies configured</td></tr>
                                </tbody>
                            </table>
                            <div v-if="throttleError" class="alert alert-error" style="margin-top:0.75rem">{{ throttleError }}</div>
                            <div v-if="throttleSuccess" class="alert alert-success" style="margin-top:0.75rem">{{ throttleSuccess }}</div>
                        </div>
                    </div>

                    <!-- Rate Limiting -->
                    <div v-if="currentPage === 'ratelimit'" id="page-ratelimit">
                        <div v-if="ratelimitLoading" class="loading">⏳ Loading…</div>
                        <div v-else>
                            <div class="card">
                                <h3>Rate Limit Configuration</h3>
                                <div v-if="ratelimitData.adminApi" class="json-block" id="ratelimit-data">{{ JSON.stringify(ratelimitData, null, 2) }}</div>
                                <div v-else class="alert alert-warning">Unable to load rate limit status</div>
                            </div>
                        </div>
                    </div>

                    <!-- DLQ Management -->
                    <div v-if="currentPage === 'dlq'" id="page-dlq">
                        <div class="card">
                            <h3>DLQ Resubmission</h3>
                            <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
                                Resubmit dead-lettered messages back to their original processing pipeline.
                                All filters are optional — omitting all fields resubmits all DLQ messages.
                            </p>
                            <div class="form-row">
                                <div class="form-group"><label>Correlation ID</label><input v-model="dlqForm.correlationId" id="dlq-correlationId" placeholder="Optional GUID" /></div>
                                <div class="form-group"><label>Message Type</label><input v-model="dlqForm.messageType" id="dlq-messageType" placeholder="Optional" /></div>
                            </div>
                            <div class="form-row">
                                <div class="form-group"><label>From Timestamp</label><input type="datetime-local" v-model="dlqForm.fromTimestamp" /></div>
                                <div class="form-group"><label>To Timestamp</label><input type="datetime-local" v-model="dlqForm.toTimestamp" /></div>
                            </div>
                            <button class="btn btn-primary" @click="resubmitDlq" :disabled="dlqSubmitting" id="btn-resubmit-dlq">
                                {{ dlqSubmitting ? '⏳ Resubmitting…' : '🔄 Resubmit' }}
                            </button>
                            <div v-if="dlqResult" class="card" style="margin-top:0.75rem;background:var(--surface2)" id="dlq-result">
                                <h3>Resubmission Result</h3>
                                <div class="json-block">{{ JSON.stringify(dlqResult, null, 2) }}</div>
                            </div>
                            <div v-if="dlqError" class="alert alert-error" style="margin-top:0.75rem">{{ dlqError }}</div>
                        </div>
                    </div>

                    <!-- Message Inspector -->
                    <div v-if="currentPage === 'messages'" id="page-messages">
                        <div class="card">
                            <h3>Message Inspector</h3>
                            <div class="form-row">
                                <div class="form-group" style="flex:2">
                                    <label>Search by Message ID, Correlation ID, or Business Key</label>
                                    <input v-model="messageQuery" id="message-query" placeholder="Enter ID or key…" @keydown.enter="searchMessages" />
                                </div>
                                <div class="form-group" style="flex:0 0 auto;display:flex;align-items:flex-end">
                                    <button class="btn btn-primary" @click="searchMessages" id="btn-search-messages">🔍 Search</button>
                                </div>
                            </div>
                            <div v-if="messageLoading" class="loading">⏳ Searching…</div>
                            <div v-if="messageResults" class="card" style="margin-top:0.75rem;background:var(--surface2)" id="message-results">
                                <h3>Results</h3>
                                <div class="json-block">{{ JSON.stringify(messageResults, null, 2) }}</div>
                            </div>
                            <div v-if="messageError" class="alert alert-error" style="margin-top:0.75rem">{{ messageError }}</div>
                        </div>
                    </div>

                    <!-- DR Drills -->
                    <div v-if="currentPage === 'dr'" id="page-dr">
                        <div class="card">
                            <h3>Execute DR Drill</h3>
                            <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
                                Run a disaster recovery drill scenario to validate failover readiness.
                            </p>
                            <div class="form-row">
                                <div class="form-group"><label>Scenario ID</label><input v-model="drForm.scenarioId" id="dr-scenarioId" placeholder="e.g. failover-us-east" /></div>
                                <div class="form-group"><label>Scenario Name</label><input v-model="drForm.name" id="dr-name" placeholder="e.g. US East Failover Drill" /></div>
                            </div>
                            <div class="form-row">
                                <div class="form-group"><label>Target Region</label><input v-model="drForm.targetRegion" id="dr-targetRegion" placeholder="e.g. us-west-2" /></div>
                                <div class="form-group"><label>Simulate Failure</label>
                                    <select v-model="drForm.simulateFailure" id="dr-simulateFailure">
                                        <option value="true">Yes</option>
                                        <option value="false">No</option>
                                    </select>
                                </div>
                            </div>
                            <button class="btn btn-danger" @click="runDrDrill" :disabled="drRunning" id="btn-run-drill">
                                {{ drRunning ? '⏳ Running…' : '🚨 Run Drill' }}
                            </button>
                            <div v-if="drResult" class="card" style="margin-top:0.75rem;background:var(--surface2)" id="dr-result">
                                <h3>Drill Result</h3>
                                <div class="json-block">{{ JSON.stringify(drResult, null, 2) }}</div>
                            </div>
                            <div v-if="drError" class="alert alert-error" style="margin-top:0.75rem">{{ drError }}</div>
                        </div>
                        <div class="card">
                            <h3>Drill History</h3>
                            <button class="btn btn-sm" style="background:var(--surface2);color:var(--text);margin-bottom:0.75rem" @click="loadDrHistory" id="btn-load-dr-history">↻ Refresh</button>
                            <div v-if="drHistoryLoading" class="loading">⏳ Loading…</div>
                            <div v-else id="dr-history">
                                <div v-if="drHistory.length" class="json-block">{{ JSON.stringify(drHistory, null, 2) }}</div>
                                <div v-else style="color:var(--muted);text-align:center;padding:1rem">No drill history available</div>
                            </div>
                        </div>
                    </div>

                    <!-- Profiling -->
                    <div v-if="currentPage === 'profiling'" id="page-profiling">
                        <div class="card">
                            <h3>Performance Snapshot</h3>
                            <div style="margin-bottom:0.75rem">
                                <button class="btn btn-primary btn-sm" @click="captureSnapshot" :disabled="profilingCapturing" id="btn-capture-snapshot">
                                    {{ profilingCapturing ? '⏳ Capturing…' : '📸 Capture Snapshot' }}
                                </button>
                                <button class="btn btn-sm" style="background:var(--surface2);color:var(--text);margin-left:0.5rem" @click="loadLatestSnapshot">↻ Load Latest</button>
                            </div>
                            <div v-if="profilingSnapshot" class="json-block" id="profiling-snapshot">{{ JSON.stringify(profilingSnapshot, null, 2) }}</div>
                            <div v-else style="color:var(--muted);text-align:center;padding:1rem">No snapshot available</div>
                        </div>
                        <div class="card">
                            <h3>GC Diagnostics</h3>
                            <button class="btn btn-sm" style="background:var(--surface2);color:var(--text);margin-bottom:0.75rem" @click="loadGcSnapshot" id="btn-load-gc">↻ Load GC Snapshot</button>
                            <div v-if="gcSnapshot" class="json-block" id="gc-snapshot">{{ JSON.stringify(gcSnapshot, null, 2) }}</div>
                            <div v-else style="color:var(--muted);text-align:center;padding:1rem">No GC data available</div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <script src="https://unpkg.com/vue@3/dist/vue.global.prod.js"></script>
        <script>
            const { createApp } = Vue;
            createApp({
                data() {
                    return {
                        currentPage: 'dashboard',
                        // Dashboard
                        status: {}, statusLoading: true, statusError: null,
                        // Throttle
                        throttlePolicies: [], throttleLoading: false, throttleError: null, throttleSuccess: null,
                        showThrottleForm: false,
                        throttleForm: { policyId: '', name: '', tenantId: '', queue: '', endpoint: '', maxMessagesPerSecond: 100, burstCapacity: 200, maxWaitTimeSeconds: 30, isEnabled: true, rejectOnBackpressure: false },
                        // Rate limiting
                        ratelimitData: {}, ratelimitLoading: true,
                        // DLQ
                        dlqForm: { correlationId: '', messageType: '', fromTimestamp: '', toTimestamp: '' },
                        dlqSubmitting: false, dlqResult: null, dlqError: null,
                        // Messages
                        messageQuery: '', messageResults: null, messageLoading: false, messageError: null,
                        // DR
                        drForm: { scenarioId: '', name: '', targetRegion: '', simulateFailure: 'false' },
                        drRunning: false, drResult: null, drError: null,
                        drHistory: [], drHistoryLoading: false,
                        // Profiling
                        profilingSnapshot: null, profilingCapturing: false,
                        gcSnapshot: null,
                    };
                },
                computed: {
                    pageTitle() {
                        const titles = {
                            dashboard: '📊 Platform Dashboard',
                            throttle: '🔧 Throttle Policies',
                            ratelimit: '🚦 Rate Limiting',
                            dlq: '📬 DLQ Management',
                            messages: '🔍 Message Inspector',
                            dr: '🛡️ DR Drills',
                            profiling: '📈 Performance Profiling',
                        };
                        return titles[this.currentPage] || 'Dashboard';
                    },
                    statusOverallClass() {
                        if (!this.status.overall) return '';
                        return this.status.overall.toLowerCase();
                    },
                },
                watch: {
                    currentPage(val) {
                        if (val === 'dashboard') this.loadStatus();
                        if (val === 'throttle') this.loadThrottlePolicies();
                        if (val === 'ratelimit') this.loadRateLimit();
                        if (val === 'dr') this.loadDrHistory();
                    },
                },
                async mounted() {
                    await this.loadStatus();
                },
                methods: {
                    async apiFetch(url, options = {}) {
                        try {
                            const res = await fetch(url, options);
                            if (!res.ok) {
                                const text = await res.text();
                                throw new Error(text || `HTTP ${res.status}`);
                            }
                            const text = await res.text();
                            return text ? JSON.parse(text) : null;
                        } catch (e) {
                            throw e;
                        }
                    },
                    formatDuration(d) {
                        if (!d) return '—';
                        if (typeof d === 'string') {
                            const parts = d.split(':');
                            if (parts.length === 3) {
                                const secs = parseFloat(parts[2]);
                                return secs < 1 ? `${(secs * 1000).toFixed(0)}ms` : `${secs.toFixed(2)}s`;
                            }
                        }
                        return String(d);
                    },
                    formatDate(d) {
                        if (!d) return '—';
                        try { return new Date(d).toLocaleString(); } catch { return d; }
                    },
                    // Dashboard
                    async loadStatus() {
                        this.statusLoading = true;
                        this.statusError = null;
                        try {
                            this.status = await this.apiFetch('/api/admin/status');
                        } catch (e) {
                            this.statusError = 'Admin API unavailable — ' + e.message;
                            this.status = { overall: 'Unhealthy', components: [], checkedAt: new Date().toISOString(), totalDuration: '00:00:00' };
                        } finally {
                            this.statusLoading = false;
                        }
                    },
                    // Throttle
                    async loadThrottlePolicies() {
                        this.throttleLoading = true;
                        this.throttleError = null;
                        this.throttleSuccess = null;
                        try {
                            this.throttlePolicies = await this.apiFetch('/api/admin/throttle/policies') || [];
                        } catch (e) {
                            this.throttleError = e.message;
                            this.throttlePolicies = [];
                        } finally {
                            this.throttleLoading = false;
                        }
                    },
                    editThrottlePolicy(p) {
                        this.throttleForm = {
                            policyId: p.policyId, name: p.name,
                            tenantId: p.partition?.tenantId || '', queue: p.partition?.queue || '',
                            endpoint: p.partition?.endpoint || '',
                            maxMessagesPerSecond: p.maxMessagesPerSecond, burstCapacity: p.burstCapacity,
                            maxWaitTimeSeconds: p.maxWaitTime ? parseInt(p.maxWaitTime.split(':').pop()) : 30,
                            isEnabled: p.isEnabled, rejectOnBackpressure: p.rejectOnBackpressure,
                        };
                        this.showThrottleForm = true;
                    },
                    async saveThrottlePolicy() {
                        this.throttleError = null;
                        this.throttleSuccess = null;
                        try {
                            await this.apiFetch('/api/admin/throttle/policies', {
                                method: 'PUT',
                                headers: { 'Content-Type': 'application/json' },
                                body: JSON.stringify(this.throttleForm),
                            });
                            this.throttleSuccess = `Policy '${this.throttleForm.policyId}' saved successfully.`;
                            this.showThrottleForm = false;
                            this.resetThrottleForm();
                            await this.loadThrottlePolicies();
                        } catch (e) {
                            this.throttleError = e.message;
                        }
                    },
                    async deleteThrottlePolicy(policyId) {
                        this.throttleError = null;
                        this.throttleSuccess = null;
                        try {
                            await fetch(`/api/admin/throttle/policies/${encodeURIComponent(policyId)}`, { method: 'DELETE' });
                            this.throttleSuccess = `Policy '${policyId}' deleted.`;
                            await this.loadThrottlePolicies();
                        } catch (e) {
                            this.throttleError = e.message;
                        }
                    },
                    resetThrottleForm() {
                        this.throttleForm = { policyId: '', name: '', tenantId: '', queue: '', endpoint: '', maxMessagesPerSecond: 100, burstCapacity: 200, maxWaitTimeSeconds: 30, isEnabled: true, rejectOnBackpressure: false };
                    },
                    // Rate limiting
                    async loadRateLimit() {
                        this.ratelimitLoading = true;
                        try {
                            this.ratelimitData = await this.apiFetch('/api/admin/ratelimit/status') || {};
                        } catch {
                            this.ratelimitData = {};
                        } finally {
                            this.ratelimitLoading = false;
                        }
                    },
                    // DLQ
                    async resubmitDlq() {
                        this.dlqSubmitting = true;
                        this.dlqResult = null;
                        this.dlqError = null;
                        try {
                            const payload = {};
                            if (this.dlqForm.correlationId) payload.correlationId = this.dlqForm.correlationId;
                            if (this.dlqForm.messageType) payload.messageType = this.dlqForm.messageType;
                            if (this.dlqForm.fromTimestamp) payload.fromTimestamp = new Date(this.dlqForm.fromTimestamp).toISOString();
                            if (this.dlqForm.toTimestamp) payload.toTimestamp = new Date(this.dlqForm.toTimestamp).toISOString();
                            this.dlqResult = await this.apiFetch('/api/admin/dlq/resubmit', {
                                method: 'POST',
                                headers: { 'Content-Type': 'application/json' },
                                body: JSON.stringify(payload),
                            });
                        } catch (e) {
                            this.dlqError = e.message;
                        } finally {
                            this.dlqSubmitting = false;
                        }
                    },
                    // Messages
                    async searchMessages() {
                        const q = this.messageQuery.trim();
                        if (!q) return;
                        this.messageLoading = true;
                        this.messageResults = null;
                        this.messageError = null;
                        try {
                            // Try as GUID first (message ID or correlation ID)
                            const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
                            if (guidRegex.test(q)) {
                                // Try message ID first
                                try {
                                    const result = await this.apiFetch(`/api/admin/messages/${q}`);
                                    if (result) { this.messageResults = result; return; }
                                } catch {}
                                // Then correlation ID
                                const results = await this.apiFetch(`/api/admin/messages/correlation/${q}`);
                                this.messageResults = results;
                            } else {
                                // Business key search via events
                                this.messageResults = await this.apiFetch(`/api/admin/events/business/${encodeURIComponent(q)}`);
                            }
                        } catch (e) {
                            this.messageError = e.message;
                        } finally {
                            this.messageLoading = false;
                        }
                    },
                    // DR Drills
                    async runDrDrill() {
                        this.drRunning = true;
                        this.drResult = null;
                        this.drError = null;
                        try {
                            this.drResult = await this.apiFetch('/api/admin/dr/drills', {
                                method: 'POST',
                                headers: { 'Content-Type': 'application/json' },
                                body: JSON.stringify({
                                    scenarioId: this.drForm.scenarioId || 'drill-' + Date.now(),
                                    name: this.drForm.name || 'Ad-hoc DR Drill',
                                    targetRegion: this.drForm.targetRegion || 'local',
                                    simulateFailure: this.drForm.simulateFailure === 'true',
                                }),
                            });
                        } catch (e) {
                            this.drError = e.message;
                        } finally {
                            this.drRunning = false;
                        }
                    },
                    async loadDrHistory() {
                        this.drHistoryLoading = true;
                        try {
                            this.drHistory = await this.apiFetch('/api/admin/dr/drills/history') || [];
                        } catch {
                            this.drHistory = [];
                        } finally {
                            this.drHistoryLoading = false;
                        }
                    },
                    // Profiling
                    async captureSnapshot() {
                        this.profilingCapturing = true;
                        try {
                            this.profilingSnapshot = await this.apiFetch('/api/admin/profiling/snapshot', { method: 'POST' });
                        } catch { }
                        finally { this.profilingCapturing = false; }
                    },
                    async loadLatestSnapshot() {
                        try {
                            this.profilingSnapshot = await this.apiFetch('/api/admin/profiling/snapshot/latest');
                        } catch { this.profilingSnapshot = null; }
                    },
                    async loadGcSnapshot() {
                        try {
                            this.gcSnapshot = await this.apiFetch('/api/admin/profiling/gc');
                        } catch { this.gcSnapshot = null; }
                    },
                },
            }).mount('#app');
        </script>
    </body>
    </html>
    """;
}
