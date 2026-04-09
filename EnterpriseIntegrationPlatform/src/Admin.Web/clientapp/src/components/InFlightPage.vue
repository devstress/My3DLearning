<template>
  <div id="page-inflight">
    <div class="card">
      <h3>In-Flight Messages</h3>
      <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
        Monitor messages currently being processed. Like BizTalk's Service Instances view,
        this shows what's actively flowing through the integration pipeline.
      </p>
      <div style="margin-bottom:0.75rem">
        <button class="btn btn-sm btn-refresh" @click="loadInFlight" id="btn-refresh-inflight">↻ Refresh</button>
        <label style="margin-left:1rem;font-size:0.8rem;color:var(--muted)">
          <input type="checkbox" v-model="autoRefresh" @change="toggleAutoRefresh" id="inflight-auto-refresh" />
          Auto-refresh (5s)
        </label>
      </div>
    </div>

    <div class="card-grid" id="inflight-stats">
      <div class="stat-card">
        <div class="label">Total In-Flight</div>
        <div class="value" :class="totalInFlight > 0 ? '' : 'idle'" id="inflight-total">{{ totalInFlight }}</div>
      </div>
      <div class="stat-card">
        <div class="label">Pending</div>
        <div class="value" style="color:var(--muted)">{{ countByStatus('Pending') }}</div>
      </div>
      <div class="stat-card">
        <div class="label">Processing</div>
        <div class="value" style="color:var(--accent)">{{ countByStatus('InFlight') }}</div>
      </div>
      <div class="stat-card">
        <div class="label">Retrying</div>
        <div class="value" style="color:var(--yellow)">{{ countByStatus('Retrying') }}</div>
      </div>
    </div>

    <div v-if="inflightLoading && !inflightData.length" class="loading">⏳ Loading…</div>

    <div v-if="inflightData.length" class="card" id="inflight-breakdown">
      <h3>Breakdown by Message Type</h3>
      <table>
        <thead><tr><th>Message Type</th><th>Count</th><th>Status</th><th>Oldest</th></tr></thead>
        <tbody>
          <tr v-for="item in inflightData" :key="item.messageType + item.status">
            <td>{{ item.messageType }}</td>
            <td><strong>{{ item.count }}</strong></td>
            <td><span class="badge" :class="'badge-' + statusClass(item.status)">{{ item.status }}</span></td>
            <td style="font-size:0.8rem;color:var(--muted)">{{ formatDate(item.oldestTimestamp) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else-if="!inflightLoading" class="card" style="text-align:center;color:var(--muted);padding:2rem">
      No messages currently in-flight. The pipeline is idle.
    </div>

    <div v-if="inflightError" class="alert alert-error">{{ inflightError }}</div>
  </div>
</template>

<script>
import { apiFetch, formatDate } from '../api.js'

export default {
  name: 'InFlightPage',
  data() {
    return {
      inflightData: [],
      inflightLoading: false,
      inflightError: null,
      autoRefresh: false,
      refreshTimer: null,
    }
  },
  computed: {
    totalInFlight() {
      return this.inflightData.reduce((sum, item) => sum + (item.count || 0), 0)
    },
  },
  async mounted() { await this.loadInFlight() },
  beforeUnmount() { this.stopAutoRefresh() },
  methods: {
    formatDate,
    statusClass(status) {
      const s = (status || '').toLowerCase()
      if (s === 'inflight') return 'inflight'
      if (s === 'retrying') return 'retrying'
      if (s === 'pending') return 'pending'
      return 'enabled'
    },
    countByStatus(status) {
      return this.inflightData.filter(d => d.status === status).reduce((sum, d) => sum + (d.count || 0), 0)
    },
    toggleAutoRefresh() {
      if (this.autoRefresh) {
        this.refreshTimer = setInterval(() => this.loadInFlight(), 5000)
      } else {
        this.stopAutoRefresh()
      }
    },
    stopAutoRefresh() {
      if (this.refreshTimer) { clearInterval(this.refreshTimer); this.refreshTimer = null }
    },
    async loadInFlight() {
      this.inflightLoading = true; this.inflightError = null
      try {
        this.inflightData = await apiFetch('/api/admin/messages/inflight') || []
      } catch (e) { this.inflightError = e.message; this.inflightData = [] }
      finally { this.inflightLoading = false }
    },
  },
}
</script>

<style scoped>
.idle { color: var(--muted); }
.badge-inflight { background: var(--accent); color: #000; }
.badge-retrying { background: var(--yellow); color: #000; }
.badge-pending { background: var(--muted); color: #000; }
</style>
