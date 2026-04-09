<template>
  <div id="page-auditlog">
    <div class="card">
      <h3>Audit Log</h3>
      <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
        Immutable record of all administrative actions. Like BizTalk's audit trail,
        every API call is logged with action, target, API key, and timestamp.
      </p>
      <div class="form-row">
        <div class="form-group">
          <label>Filter by Action</label>
          <input v-model="actionFilter" id="audit-action-filter" placeholder="e.g. GetPlatformStatus" />
        </div>
        <div class="form-group">
          <label>Filter by API Key</label>
          <input v-model="apiKeyFilter" id="audit-apikey-filter" placeholder="e.g. admin-****" />
        </div>
        <div class="form-group" style="flex:0 0 auto;display:flex;align-items:flex-end;gap:0.5rem">
          <button class="btn btn-primary btn-sm" @click="loadAuditLog" id="btn-search-audit">🔍 Search</button>
          <button class="btn btn-sm btn-refresh" @click="clearAndLoad" id="btn-refresh-audit">↻ Refresh</button>
        </div>
      </div>
    </div>

    <div v-if="auditLoading" class="loading">⏳ Loading audit log…</div>

    <div v-if="auditEntries.length" class="card" id="audit-entries">
      <table>
        <thead><tr><th>Timestamp</th><th>Action</th><th>Target</th><th>API Key</th></tr></thead>
        <tbody>
          <tr v-for="(entry, i) in auditEntries" :key="i">
            <td style="font-size:0.8rem;color:var(--muted);white-space:nowrap">{{ formatDate(entry.timestamp) }}</td>
            <td><span class="badge badge-enabled">{{ entry.action }}</span></td>
            <td>{{ entry.targetId || '—' }}</td>
            <td style="font-family:monospace;font-size:0.8rem">{{ entry.apiKey || '****' }}</td>
          </tr>
        </tbody>
      </table>
      <div v-if="hasMore" style="text-align:center;margin-top:0.75rem">
        <button class="btn btn-sm btn-refresh" @click="loadMore" id="btn-load-more-audit">Load More</button>
      </div>
    </div>
    <div v-else-if="!auditLoading" class="card" style="text-align:center;color:var(--muted);padding:2rem">
      No audit entries found. Try adjusting your filters or refresh.
    </div>

    <div v-if="auditError" class="alert alert-error">{{ auditError }}</div>

    <div class="card-grid">
      <div class="stat-card">
        <div class="label">Entries Shown</div>
        <div class="value">{{ auditEntries.length }}</div>
      </div>
      <div class="stat-card">
        <div class="label">Unique Actions</div>
        <div class="value">{{ uniqueActions.length }}</div>
      </div>
    </div>
  </div>
</template>

<script>
import { apiFetch, formatDate } from '../api.js'

export default {
  name: 'AuditLogPage',
  data() {
    return {
      auditEntries: [],
      auditLoading: false,
      auditError: null,
      actionFilter: '',
      apiKeyFilter: '',
      page: 1,
      hasMore: false,
    }
  },
  computed: {
    uniqueActions() {
      return [...new Set(this.auditEntries.map(e => e.action).filter(Boolean))]
    },
  },
  async mounted() { await this.loadAuditLog() },
  methods: {
    formatDate,
    clearAndLoad() {
      this.actionFilter = ''; this.apiKeyFilter = ''; this.page = 1
      this.loadAuditLog()
    },
    async loadMore() {
      this.page++
      await this.loadAuditLog(true)
    },
    async loadAuditLog(append = false) {
      this.auditLoading = true; this.auditError = null
      try {
        const params = new URLSearchParams()
        if (this.actionFilter) params.set('action', this.actionFilter)
        if (this.apiKeyFilter) params.set('apiKey', this.apiKeyFilter)
        params.set('page', this.page)
        params.set('pageSize', 50)
        const url = `/api/admin/audit?${params.toString()}`
        const result = await apiFetch(url) || []
        if (append) {
          this.auditEntries = [...this.auditEntries, ...result]
        } else {
          this.auditEntries = result
        }
        this.hasMore = result.length >= 50
      } catch (e) { this.auditError = e.message; if (!append) this.auditEntries = [] }
      finally { this.auditLoading = false }
    },
  },
}
</script>
