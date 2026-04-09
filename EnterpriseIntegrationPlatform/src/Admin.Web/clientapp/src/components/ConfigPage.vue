<template>
  <div id="page-config">
    <div class="card">
      <h3>Configuration Store</h3>
      <div style="margin-bottom:0.75rem">
        <button class="btn btn-primary btn-sm" @click="showConfigForm = !showConfigForm" id="btn-add-config">
          {{ showConfigForm ? '✕ Cancel' : '＋ Add Entry' }}
        </button>
        <button class="btn btn-sm btn-refresh" @click="loadConfig">↻ Refresh</button>
        <select v-model="envFilter" style="margin-left:0.5rem;padding:0.3rem;background:var(--surface2);color:var(--text);border:1px solid var(--border);border-radius:0.4rem;font-size:0.8rem" @change="loadConfig" id="config-env-filter">
          <option value="">All Environments</option>
          <option value="default">default</option>
          <option value="development">development</option>
          <option value="staging">staging</option>
          <option value="production">production</option>
        </select>
      </div>
      <div v-if="showConfigForm" class="card" style="background:var(--surface2)" id="config-form">
        <h3>{{ configForm.isEdit ? 'Edit Entry' : 'New Entry' }}</h3>
        <div class="form-row">
          <div class="form-group"><label>Key</label><input v-model="configForm.key" id="config-key" :disabled="configForm.isEdit" /></div>
          <div class="form-group"><label>Environment</label><input v-model="configForm.environment" id="config-env" placeholder="default" /></div>
        </div>
        <div class="form-group">
          <label>Value</label>
          <textarea v-model="configForm.value" id="config-value" rows="3"></textarea>
        </div>
        <button class="btn btn-success btn-sm" @click="saveConfig" id="btn-save-config">💾 Save</button>
      </div>
      <div v-if="configLoading" class="loading">⏳ Loading…</div>
      <table v-else id="config-table">
        <thead><tr><th>Key</th><th>Value</th><th>Environment</th><th>Modified By</th><th>Actions</th></tr></thead>
        <tbody>
          <tr v-for="c in configEntries" :key="c.key + c.environment">
            <td>{{ c.key }}</td>
            <td style="max-width:300px;overflow:hidden;text-overflow:ellipsis">{{ c.value }}</td>
            <td><span class="badge badge-enabled">{{ c.environment }}</span></td>
            <td>{{ c.modifiedBy || '—' }}</td>
            <td>
              <button class="btn btn-sm btn-warning" @click="editConfig(c)">✏️</button>
              <button class="btn btn-sm btn-danger" @click="deleteConfig(c.key, c.environment)">🗑️</button>
            </td>
          </tr>
          <tr v-if="!configEntries.length"><td colspan="5" style="text-align:center;color:var(--muted)">No configuration entries</td></tr>
        </tbody>
      </table>
      <div v-if="configError" class="alert alert-error" style="margin-top:0.75rem">{{ configError }}</div>
      <div v-if="configSuccess" class="alert alert-success" style="margin-top:0.75rem">{{ configSuccess }}</div>
    </div>
  </div>
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'ConfigPage',
  data() {
    return {
      configEntries: [],
      configLoading: false,
      configError: null,
      configSuccess: null,
      showConfigForm: false,
      envFilter: '',
      configForm: { key: '', value: '', environment: 'default', isEdit: false },
    }
  },
  async mounted() { await this.loadConfig() },
  methods: {
    async loadConfig() {
      this.configLoading = true
      this.configError = null
      this.configSuccess = null
      try {
        const url = this.envFilter ? `/api/admin/config?environment=${this.envFilter}` : '/api/admin/config'
        this.configEntries = await apiFetch(url) || []
      } catch (e) { this.configError = e.message; this.configEntries = [] }
      finally { this.configLoading = false }
    },
    editConfig(c) {
      this.configForm = { key: c.key, value: c.value, environment: c.environment, isEdit: true }
      this.showConfigForm = true
    },
    async saveConfig() {
      this.configError = null; this.configSuccess = null
      try {
        await apiFetch(`/api/admin/config/${encodeURIComponent(this.configForm.key)}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ value: this.configForm.value, environment: this.configForm.environment || 'default' }),
        })
        this.configSuccess = `Entry '${this.configForm.key}' saved.`
        this.showConfigForm = false
        this.configForm = { key: '', value: '', environment: 'default', isEdit: false }
        await this.loadConfig()
      } catch (e) { this.configError = e.message }
    },
    async deleteConfig(key, env) {
      this.configError = null; this.configSuccess = null
      try {
        await apiFetch(`/api/admin/config/${encodeURIComponent(key)}?environment=${env}`, { method: 'DELETE' })
        this.configSuccess = `Entry '${key}' deleted.`
        await this.loadConfig()
      } catch (e) { this.configError = e.message }
    },
  },
}
</script>
