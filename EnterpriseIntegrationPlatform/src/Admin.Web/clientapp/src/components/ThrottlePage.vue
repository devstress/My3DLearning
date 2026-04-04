<template>
  <div id="page-throttle">
    <div class="card">
      <h3>Throttle Policies</h3>
      <div style="margin-bottom:0.75rem">
        <button class="btn btn-primary btn-sm" @click="showThrottleForm = !showThrottleForm" id="btn-add-throttle">
          {{ showThrottleForm ? '✕ Cancel' : '＋ Add Policy' }}
        </button>
        <button class="btn btn-sm btn-refresh" @click="loadThrottlePolicies">↻ Refresh</button>
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
        <thead>
          <tr><th>Policy ID</th><th>Name</th><th>Tenant</th><th>Queue</th><th>Max Msg/s</th><th>Burst</th><th>Status</th><th>Actions</th></tr>
        </thead>
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
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'ThrottlePage',
  data() {
    return {
      throttlePolicies: [],
      throttleLoading: false,
      throttleError: null,
      throttleSuccess: null,
      showThrottleForm: false,
      throttleForm: {
        policyId: '', name: '', tenantId: '', queue: '', endpoint: '',
        maxMessagesPerSecond: 100, burstCapacity: 200, maxWaitTimeSeconds: 30,
        isEnabled: true, rejectOnBackpressure: false,
      },
    }
  },
  async mounted() {
    await this.loadThrottlePolicies()
  },
  methods: {
    async loadThrottlePolicies() {
      this.throttleLoading = true
      this.throttleError = null
      this.throttleSuccess = null
      try {
        this.throttlePolicies = await apiFetch('/api/admin/throttle/policies') || []
      } catch (e) {
        this.throttleError = e.message
        this.throttlePolicies = []
      } finally {
        this.throttleLoading = false
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
      }
      this.showThrottleForm = true
    },
    async saveThrottlePolicy() {
      this.throttleError = null
      this.throttleSuccess = null
      try {
        await apiFetch('/api/admin/throttle/policies', {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(this.throttleForm),
        })
        this.throttleSuccess = `Policy '${this.throttleForm.policyId}' saved successfully.`
        this.showThrottleForm = false
        this.resetThrottleForm()
        await this.loadThrottlePolicies()
      } catch (e) {
        this.throttleError = e.message
      }
    },
    async deleteThrottlePolicy(policyId) {
      this.throttleError = null
      this.throttleSuccess = null
      try {
        await fetch(`/api/admin/throttle/policies/${encodeURIComponent(policyId)}`, { method: 'DELETE' })
        this.throttleSuccess = `Policy '${policyId}' deleted.`
        await this.loadThrottlePolicies()
      } catch (e) {
        this.throttleError = e.message
      }
    },
    resetThrottleForm() {
      this.throttleForm = {
        policyId: '', name: '', tenantId: '', queue: '', endpoint: '',
        maxMessagesPerSecond: 100, burstCapacity: 200, maxWaitTimeSeconds: 30,
        isEnabled: true, rejectOnBackpressure: false,
      }
    },
  },
}
</script>
