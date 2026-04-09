<template>
  <div id="page-features">
    <div class="card">
      <h3>Feature Flags</h3>
      <div style="margin-bottom:0.75rem">
        <button class="btn btn-primary btn-sm" @click="showFlagForm = !showFlagForm" id="btn-add-flag">
          {{ showFlagForm ? '✕ Cancel' : '＋ Add Flag' }}
        </button>
        <button class="btn btn-sm btn-refresh" @click="loadFlags">↻ Refresh</button>
      </div>
      <div v-if="showFlagForm" class="card" style="background:var(--surface2)" id="flag-form">
        <h3>{{ flagForm.isEdit ? 'Edit Flag' : 'New Flag' }}</h3>
        <div class="form-row">
          <div class="form-group"><label>Name</label><input v-model="flagForm.name" id="flag-name" :disabled="flagForm.isEdit" /></div>
          <div class="form-group"><label>Rollout %</label><input type="number" v-model.number="flagForm.rolloutPercentage" id="flag-rollout" min="0" max="100" /></div>
        </div>
        <div class="form-row">
          <div class="form-group"><label><input type="checkbox" v-model="flagForm.isEnabled" /> Enabled</label></div>
          <div class="form-group"><label>Target Tenants (comma-separated)</label><input v-model="flagForm.targetTenantsStr" id="flag-tenants" /></div>
        </div>
        <button class="btn btn-success btn-sm" @click="saveFlag" id="btn-save-flag">💾 Save</button>
      </div>
      <div v-if="flagsLoading" class="loading">⏳ Loading…</div>
      <table v-else id="flags-table">
        <thead><tr><th>Name</th><th>Status</th><th>Rollout</th><th>Target Tenants</th><th>Actions</th></tr></thead>
        <tbody>
          <tr v-for="f in flags" :key="f.name">
            <td>{{ f.name }}</td>
            <td><span class="badge" :class="f.isEnabled ? 'badge-enabled' : 'badge-disabled'">{{ f.isEnabled ? 'Enabled' : 'Disabled' }}</span></td>
            <td>{{ f.rolloutPercentage }}%</td>
            <td>{{ (f.targetTenants || []).join(', ') || '—' }}</td>
            <td>
              <button class="btn btn-sm btn-warning" @click="editFlag(f)">✏️</button>
              <button class="btn btn-sm btn-danger" @click="deleteFlag(f.name)">🗑️</button>
            </td>
          </tr>
          <tr v-if="!flags.length"><td colspan="5" style="text-align:center;color:var(--muted)">No feature flags</td></tr>
        </tbody>
      </table>
      <div v-if="flagsError" class="alert alert-error" style="margin-top:0.75rem">{{ flagsError }}</div>
      <div v-if="flagsSuccess" class="alert alert-success" style="margin-top:0.75rem">{{ flagsSuccess }}</div>
    </div>
  </div>
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'FeatureFlagsPage',
  data() {
    return {
      flags: [],
      flagsLoading: false,
      flagsError: null,
      flagsSuccess: null,
      showFlagForm: false,
      flagForm: { name: '', isEnabled: false, rolloutPercentage: 100, targetTenantsStr: '', isEdit: false },
    }
  },
  async mounted() { await this.loadFlags() },
  methods: {
    async loadFlags() {
      this.flagsLoading = true; this.flagsError = null; this.flagsSuccess = null
      try { this.flags = await apiFetch('/api/admin/features') || [] }
      catch (e) { this.flagsError = e.message; this.flags = [] }
      finally { this.flagsLoading = false }
    },
    editFlag(f) {
      this.flagForm = {
        name: f.name, isEnabled: f.isEnabled,
        rolloutPercentage: f.rolloutPercentage,
        targetTenantsStr: (f.targetTenants || []).join(', '),
        isEdit: true,
      }
      this.showFlagForm = true
    },
    async saveFlag() {
      this.flagsError = null; this.flagsSuccess = null
      const tenants = this.flagForm.targetTenantsStr ? this.flagForm.targetTenantsStr.split(',').map(t => t.trim()).filter(Boolean) : []
      try {
        await apiFetch(`/api/admin/features/${encodeURIComponent(this.flagForm.name)}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ isEnabled: this.flagForm.isEnabled, rolloutPercentage: this.flagForm.rolloutPercentage, targetTenants: tenants }),
        })
        this.flagsSuccess = `Flag '${this.flagForm.name}' saved.`
        this.showFlagForm = false
        this.flagForm = { name: '', isEnabled: false, rolloutPercentage: 100, targetTenantsStr: '', isEdit: false }
        await this.loadFlags()
      } catch (e) { this.flagsError = e.message }
    },
    async deleteFlag(name) {
      this.flagsError = null; this.flagsSuccess = null
      try {
        await apiFetch(`/api/admin/features/${encodeURIComponent(name)}`, { method: 'DELETE' })
        this.flagsSuccess = `Flag '${name}' deleted.`
        await this.loadFlags()
      } catch (e) { this.flagsError = e.message }
    },
  },
}
</script>
