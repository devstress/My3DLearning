<template>
  <div id="page-dashboard">
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
          <thead>
            <tr><th>Component</th><th>Status</th><th>Duration</th><th>Description</th></tr>
          </thead>
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
</template>

<script>
import { apiFetch, formatDuration, formatDate } from '../api.js'

export default {
  name: 'DashboardPage',
  data() {
    return {
      status: {},
      statusLoading: true,
      statusError: null,
    }
  },
  computed: {
    statusOverallClass() {
      if (!this.status.overall) return ''
      return this.status.overall.toLowerCase()
    },
  },
  async mounted() {
    await this.loadStatus()
  },
  methods: {
    formatDuration,
    formatDate,
    async loadStatus() {
      this.statusLoading = true
      this.statusError = null
      try {
        this.status = await apiFetch('/api/admin/status')
      } catch (e) {
        this.statusError = 'Admin API unavailable — ' + e.message
        this.status = { overall: 'Unhealthy', components: [], checkedAt: new Date().toISOString(), totalDuration: '00:00:00' }
      } finally {
        this.statusLoading = false
      }
    },
  },
}
</script>
