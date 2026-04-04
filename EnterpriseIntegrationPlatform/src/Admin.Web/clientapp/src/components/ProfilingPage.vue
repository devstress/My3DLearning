<template>
  <div id="page-profiling">
    <div class="card">
      <h3>Performance Snapshot</h3>
      <div style="margin-bottom:0.75rem">
        <button class="btn btn-primary btn-sm" @click="captureSnapshot" :disabled="profilingCapturing" id="btn-capture-snapshot">
          {{ profilingCapturing ? '⏳ Capturing…' : '📸 Capture Snapshot' }}
        </button>
        <button class="btn btn-sm btn-refresh" @click="loadLatestSnapshot">↻ Load Latest</button>
      </div>
      <div v-if="profilingSnapshot" class="json-block" id="profiling-snapshot">{{ JSON.stringify(profilingSnapshot, null, 2) }}</div>
      <div v-else style="color:var(--muted);text-align:center;padding:1rem">No snapshot available</div>
    </div>
    <div class="card">
      <h3>GC Diagnostics</h3>
      <button class="btn btn-sm btn-refresh" style="margin-bottom:0.75rem" @click="loadGcSnapshot" id="btn-load-gc">↻ Load GC Snapshot</button>
      <div v-if="gcSnapshot" class="json-block" id="gc-snapshot">{{ JSON.stringify(gcSnapshot, null, 2) }}</div>
      <div v-else style="color:var(--muted);text-align:center;padding:1rem">No GC data available</div>
    </div>
  </div>
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'ProfilingPage',
  data() {
    return {
      profilingSnapshot: null,
      profilingCapturing: false,
      gcSnapshot: null,
    }
  },
  methods: {
    async captureSnapshot() {
      this.profilingCapturing = true
      try {
        this.profilingSnapshot = await apiFetch('/api/admin/profiling/snapshot', { method: 'POST' })
      } catch { /* ignore */ }
      finally { this.profilingCapturing = false }
    },
    async loadLatestSnapshot() {
      try {
        this.profilingSnapshot = await apiFetch('/api/admin/profiling/snapshot/latest')
      } catch { this.profilingSnapshot = null }
    },
    async loadGcSnapshot() {
      try {
        this.gcSnapshot = await apiFetch('/api/admin/profiling/gc')
      } catch { this.gcSnapshot = null }
    },
  },
}
</script>
