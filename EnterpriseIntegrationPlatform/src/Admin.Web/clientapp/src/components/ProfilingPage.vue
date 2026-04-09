<template>
  <div id="page-profiling">
    <!-- Performance Snapshot -->
    <div class="card">
      <h3>Performance Snapshot</h3>
      <div style="margin-bottom:0.75rem">
        <button class="btn btn-primary btn-sm" @click="captureSnapshot" :disabled="profilingCapturing" id="btn-capture-snapshot">
          {{ profilingCapturing ? '⏳ Capturing…' : '📸 Capture Snapshot' }}
        </button>
        <button class="btn btn-sm btn-refresh" @click="loadLatestSnapshot">↻ Load Latest</button>
      </div>
      <div v-if="profilingSnapshot" id="profiling-snapshot">
        <div class="card-grid">
          <div class="stat-card">
            <div class="label">Label</div>
            <div class="value" style="font-size:0.9rem">{{ profilingSnapshot.label || '—' }}</div>
          </div>
          <div class="stat-card">
            <div class="label">Timestamp</div>
            <div class="value" style="font-size:0.85rem">{{ formatDate(profilingSnapshot.capturedAt) }}</div>
          </div>
          <div class="stat-card">
            <div class="label">Heap (MB)</div>
            <div class="value">{{ formatMb(profilingSnapshot.heapSizeBytes) }}</div>
          </div>
          <div class="stat-card">
            <div class="label">Working Set (MB)</div>
            <div class="value">{{ formatMb(profilingSnapshot.workingSetBytes) }}</div>
          </div>
        </div>
        <details style="margin-top:0.5rem">
          <summary style="cursor:pointer;color:var(--muted);font-size:0.85rem">Raw JSON</summary>
          <div class="json-block" style="margin-top:0.5rem">{{ JSON.stringify(profilingSnapshot, null, 2) }}</div>
        </details>
      </div>
      <div v-else style="color:var(--muted);text-align:center;padding:1rem">No snapshot available</div>
    </div>

    <!-- Hotspot Analysis -->
    <div class="card">
      <h3>🔥 Hotspot Analysis</h3>
      <button class="btn btn-sm btn-refresh" style="margin-bottom:0.75rem" @click="loadHotspots" id="btn-load-hotspots">↻ Detect Hotspots</button>
      <div v-if="hotspots.length" id="hotspot-table">
        <table>
          <thead><tr><th>Operation</th><th>Severity</th><th>Avg Duration</th><th>Call Count</th><th>Reason</th></tr></thead>
          <tbody>
            <tr v-for="h in hotspots" :key="h.operationName">
              <td><strong>{{ h.operationName }}</strong></td>
              <td><span class="badge" :class="severityBadge(h.severity)">{{ h.severity }}</span></td>
              <td>{{ h.averageDurationMs?.toFixed(1) }}ms</td>
              <td>{{ h.callCount }}</td>
              <td style="font-size:0.85rem;color:var(--muted)">{{ h.reason || '—' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-else style="color:var(--muted);text-align:center;padding:1rem">No hotspots detected. Click Detect Hotspots to analyze.</div>
    </div>

    <!-- Operation Statistics -->
    <div class="card">
      <h3>📊 Operation Statistics</h3>
      <button class="btn btn-sm btn-refresh" style="margin-bottom:0.75rem" @click="loadOperations" id="btn-load-ops">↻ Load Operations</button>
      <div v-if="operations.length" id="operations-table">
        <table>
          <thead><tr><th>Operation</th><th>Calls</th><th>Avg (ms)</th><th>P95 (ms)</th><th>Max (ms)</th><th>Errors</th></tr></thead>
          <tbody>
            <tr v-for="op in operations" :key="op.name">
              <td>{{ op.name }}</td>
              <td>{{ op.callCount }}</td>
              <td>{{ op.averageDurationMs?.toFixed(1) }}</td>
              <td>{{ op.p95DurationMs?.toFixed(1) }}</td>
              <td>{{ op.maxDurationMs?.toFixed(1) }}</td>
              <td :style="op.errorCount > 0 ? 'color:var(--red)' : ''">{{ op.errorCount || 0 }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-else style="color:var(--muted);text-align:center;padding:1rem">No operation data available</div>
    </div>

    <!-- GC Diagnostics -->
    <div class="card">
      <h3>🗑️ GC Diagnostics</h3>
      <div style="margin-bottom:0.75rem">
        <button class="btn btn-sm btn-refresh" @click="loadGcSnapshot" id="btn-load-gc">↻ Load GC Snapshot</button>
        <button class="btn btn-sm btn-refresh" @click="loadGcRecommendations" style="margin-left:0.25rem" id="btn-gc-recs">💡 Recommendations</button>
      </div>
      <div v-if="gcSnapshot" id="gc-snapshot">
        <div class="card-grid">
          <div class="stat-card">
            <div class="label">Gen0 Collections</div>
            <div class="value">{{ gcSnapshot.gen0Collections ?? '—' }}</div>
          </div>
          <div class="stat-card">
            <div class="label">Gen1 Collections</div>
            <div class="value">{{ gcSnapshot.gen1Collections ?? '—' }}</div>
          </div>
          <div class="stat-card">
            <div class="label">Gen2 Collections</div>
            <div class="value">{{ gcSnapshot.gen2Collections ?? '—' }}</div>
          </div>
          <div class="stat-card">
            <div class="label">Total Allocated (MB)</div>
            <div class="value">{{ formatMb(gcSnapshot.totalAllocatedBytes) }}</div>
          </div>
        </div>
        <details style="margin-top:0.5rem">
          <summary style="cursor:pointer;color:var(--muted);font-size:0.85rem">Raw JSON</summary>
          <div class="json-block" style="margin-top:0.5rem">{{ JSON.stringify(gcSnapshot, null, 2) }}</div>
        </details>
      </div>
      <div v-else style="color:var(--muted);text-align:center;padding:1rem">No GC data available</div>
      <div v-if="gcRecommendations.length" class="card" style="margin-top:0.75rem;background:var(--surface2)" id="gc-recommendations">
        <h3>💡 GC Recommendations</h3>
        <ul style="margin:0;padding-left:1.5rem">
          <li v-for="(rec, i) in gcRecommendations" :key="i" style="margin-bottom:0.5rem;font-size:0.9rem">{{ rec }}</li>
        </ul>
      </div>
    </div>

    <!-- Benchmark Baselines -->
    <div class="card">
      <h3>📈 Benchmark Baselines</h3>
      <button class="btn btn-sm btn-refresh" style="margin-bottom:0.75rem" @click="loadBenchmarks" id="btn-load-benchmarks">↻ Load Benchmarks</button>
      <div v-if="benchmarks.length" id="benchmark-table">
        <table>
          <thead><tr><th>Benchmark</th><th>Baseline (ms)</th><th>Current (ms)</th><th>Δ%</th><th>Status</th></tr></thead>
          <tbody>
            <tr v-for="b in benchmarks" :key="b.name">
              <td>{{ b.name }}</td>
              <td>{{ b.baselineMs?.toFixed(1) }}</td>
              <td>{{ b.currentMs?.toFixed(1) }}</td>
              <td :style="b.regressionPct > 10 ? 'color:var(--red)' : b.regressionPct < -5 ? 'color:var(--green)' : ''">
                {{ b.regressionPct != null ? (b.regressionPct > 0 ? '+' : '') + b.regressionPct.toFixed(1) + '%' : '—' }}
              </td>
              <td><span class="badge" :class="b.regressionPct > 10 ? 'badge-unhealthy' : 'badge-healthy'">{{ b.regressionPct > 10 ? 'Regressed' : 'OK' }}</span></td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-else style="color:var(--muted);text-align:center;padding:1rem">No benchmark baselines available</div>
    </div>
  </div>
</template>

<script>
import { apiFetch, formatDate } from '../api.js'

export default {
  name: 'ProfilingPage',
  data() {
    return {
      profilingSnapshot: null,
      profilingCapturing: false,
      gcSnapshot: null,
      gcRecommendations: [],
      hotspots: [],
      operations: [],
      benchmarks: [],
    }
  },
  methods: {
    formatDate,
    formatMb(bytes) {
      if (bytes == null) return '—'
      return (bytes / (1024 * 1024)).toFixed(1)
    },
    severityBadge(severity) {
      if (severity === 'Critical') return 'badge-unhealthy'
      if (severity === 'Warning') return 'badge-warning'
      return 'badge-enabled'
    },
    async captureSnapshot() {
      this.profilingCapturing = true
      try { this.profilingSnapshot = await apiFetch('/api/admin/profiling/snapshot', { method: 'POST' }) }
      catch { /* ignore */ }
      finally { this.profilingCapturing = false }
    },
    async loadLatestSnapshot() {
      try { this.profilingSnapshot = await apiFetch('/api/admin/profiling/snapshot/latest') }
      catch { this.profilingSnapshot = null }
    },
    async loadHotspots() {
      try { this.hotspots = await apiFetch('/api/admin/profiling/hotspots') || [] }
      catch { this.hotspots = [] }
    },
    async loadOperations() {
      try { this.operations = await apiFetch('/api/admin/profiling/operations') || [] }
      catch { this.operations = [] }
    },
    async loadGcSnapshot() {
      try { this.gcSnapshot = await apiFetch('/api/admin/profiling/gc') }
      catch { this.gcSnapshot = null }
    },
    async loadGcRecommendations() {
      try { this.gcRecommendations = await apiFetch('/api/admin/profiling/gc/recommendations') || [] }
      catch { this.gcRecommendations = [] }
    },
    async loadBenchmarks() {
      try { this.benchmarks = await apiFetch('/api/admin/profiling/benchmarks') || [] }
      catch { this.benchmarks = [] }
    },
  },
}
</script>
