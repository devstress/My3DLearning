<template>
  <div id="page-connectors">
    <div class="card">
      <h3>Connector Health</h3>
      <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
        Monitor registered connectors across the platform.
        Like BizTalk's Adapter and Port management, this shows all integration endpoints
        with their type, status, and health.
      </p>
      <div style="margin-bottom:0.75rem">
        <button class="btn btn-sm btn-refresh" @click="loadConnectors" id="btn-refresh-connectors">↻ Refresh</button>
        <select v-model="typeFilter" style="margin-left:0.5rem;padding:0.3rem;background:var(--surface2);color:var(--text);border:1px solid var(--border);border-radius:0.4rem;font-size:0.8rem" id="conn-type-filter">
          <option value="">All Types</option>
          <option value="Http">HTTP</option>
          <option value="Sftp">SFTP</option>
          <option value="Email">Email</option>
          <option value="File">File</option>
        </select>
      </div>
    </div>

    <div class="card-grid" id="connector-stats">
      <div class="stat-card">
        <div class="label">Total Connectors</div>
        <div class="value">{{ connectors.length }}</div>
      </div>
      <div class="stat-card">
        <div class="label">Healthy</div>
        <div class="value" style="color:var(--green)">{{ healthyCount }}</div>
      </div>
      <div class="stat-card">
        <div class="label">Degraded</div>
        <div class="value" style="color:var(--yellow)">{{ degradedCount }}</div>
      </div>
      <div class="stat-card">
        <div class="label">Unhealthy</div>
        <div class="value" style="color:var(--red)">{{ unhealthyCount }}</div>
      </div>
    </div>

    <div v-if="connLoading" class="loading">⏳ Loading connectors…</div>

    <div v-if="filteredConnectors.length" class="card" id="connector-table">
      <table>
        <thead><tr><th>Name</th><th>Type</th><th>Status</th><th>Last Check</th><th>Description</th></tr></thead>
        <tbody>
          <tr v-for="c in filteredConnectors" :key="c.name">
            <td><strong>{{ c.name }}</strong></td>
            <td><span class="badge badge-enabled">{{ c.connectorType }}</span></td>
            <td><span class="badge" :class="healthBadgeClass(c.healthStatus)">{{ c.healthStatus || 'Unknown' }}</span></td>
            <td style="font-size:0.8rem;color:var(--muted)">{{ formatDate(c.lastChecked) }}</td>
            <td style="font-size:0.85rem;color:var(--muted)">{{ c.description || '—' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else-if="!connLoading" class="card" style="text-align:center;color:var(--muted);padding:2rem">
      No connectors registered. Connectors will appear here when the platform has active integrations.
    </div>

    <div v-if="connError" class="alert alert-error">{{ connError }}</div>
  </div>
</template>

<script>
import { apiFetch, formatDate } from '../api.js'

export default {
  name: 'ConnectorsPage',
  data() {
    return {
      connectors: [],
      connLoading: false,
      connError: null,
      typeFilter: '',
    }
  },
  computed: {
    filteredConnectors() {
      if (!this.typeFilter) return this.connectors
      return this.connectors.filter(c => c.connectorType === this.typeFilter)
    },
    healthyCount() {
      return this.connectors.filter(c => c.healthStatus === 'Healthy').length
    },
    degradedCount() {
      return this.connectors.filter(c => c.healthStatus === 'Degraded').length
    },
    unhealthyCount() {
      return this.connectors.filter(c => c.healthStatus === 'Unhealthy').length
    },
  },
  async mounted() { await this.loadConnectors() },
  methods: {
    formatDate,
    healthBadgeClass(status) {
      if (status === 'Healthy') return 'badge-healthy'
      if (status === 'Degraded') return 'badge-warning'
      if (status === 'Unhealthy') return 'badge-unhealthy'
      return 'badge-disabled'
    },
    async loadConnectors() {
      this.connLoading = true; this.connError = null
      try {
        this.connectors = await apiFetch('/api/admin/connectors') || []
      } catch (e) { this.connError = e.message; this.connectors = [] }
      finally { this.connLoading = false }
    },
  },
}
</script>
