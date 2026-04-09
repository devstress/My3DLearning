<template>
  <div id="page-tenants">
    <div class="card">
      <h3>Tenant Management</h3>
      <div style="margin-bottom:0.75rem">
        <button class="btn btn-primary btn-sm" @click="showOnboardForm = !showOnboardForm" id="btn-onboard-tenant">
          {{ showOnboardForm ? '✕ Cancel' : '＋ Onboard Tenant' }}
        </button>
      </div>
      <div v-if="showOnboardForm" class="card" style="background:var(--surface2)" id="onboard-form">
        <h3>Onboard New Tenant</h3>
        <div class="form-row">
          <div class="form-group"><label>Tenant ID</label><input v-model="onboardForm.tenantId" id="tenant-id" placeholder="e.g. acme-corp" /></div>
          <div class="form-group"><label>Display Name</label><input v-model="onboardForm.displayName" id="tenant-name" placeholder="e.g. ACME Corporation" /></div>
        </div>
        <div class="form-row">
          <div class="form-group"><label>Tier</label>
            <select v-model="onboardForm.tier" id="tenant-tier">
              <option value="Free">Free</option>
              <option value="Standard">Standard</option>
              <option value="Premium">Premium</option>
              <option value="Enterprise">Enterprise</option>
            </select>
          </div>
          <div class="form-group"><label>Region</label><input v-model="onboardForm.region" id="tenant-region" placeholder="e.g. us-east-1" /></div>
        </div>
        <button class="btn btn-success btn-sm" @click="onboardTenant" :disabled="onboarding" id="btn-submit-onboard">
          {{ onboarding ? '⏳ Provisioning…' : '🚀 Provision' }}
        </button>
      </div>
    </div>

    <!-- Tenant Lookup -->
    <div class="card">
      <h3>Tenant Status & Quota</h3>
      <div class="form-row">
        <div class="form-group" style="flex:2">
          <label>Tenant ID</label>
          <input v-model="lookupTenantId" id="tenant-lookup-id" placeholder="Enter tenant ID…" @keydown.enter="lookupTenant" />
        </div>
        <div class="form-group" style="flex:0 0 auto;display:flex;align-items:flex-end;gap:0.5rem">
          <button class="btn btn-primary btn-sm" @click="lookupTenant" id="btn-lookup-tenant">🔍 Lookup</button>
          <button class="btn btn-danger btn-sm" @click="deprovisionTenant" id="btn-deprovision-tenant">🗑️ Deprovision</button>
        </div>
      </div>
      <div v-if="tenantStatus" class="card" style="background:var(--surface2);margin-top:0.75rem" id="tenant-status">
        <h3>Status</h3>
        <div class="json-block">{{ JSON.stringify(tenantStatus, null, 2) }}</div>
      </div>
      <div v-if="tenantQuota" class="card" style="background:var(--surface2);margin-top:0.75rem" id="tenant-quota">
        <h3>Quota</h3>
        <div class="json-block">{{ JSON.stringify(tenantQuota, null, 2) }}</div>
      </div>
    </div>

    <div v-if="tenantResult" class="card" id="tenant-result" style="background:var(--surface2)">
      <h3>Provisioning Result</h3>
      <div class="json-block">{{ JSON.stringify(tenantResult, null, 2) }}</div>
    </div>
    <div v-if="tenantError" class="alert alert-error">{{ tenantError }}</div>
    <div v-if="tenantSuccess" class="alert alert-success">{{ tenantSuccess }}</div>
  </div>
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'TenantsPage',
  data() {
    return {
      showOnboardForm: false,
      onboardForm: { tenantId: '', displayName: '', tier: 'Standard', region: '' },
      onboarding: false,
      tenantResult: null,
      lookupTenantId: '',
      tenantStatus: null,
      tenantQuota: null,
      tenantError: null,
      tenantSuccess: null,
    }
  },
  methods: {
    async onboardTenant() {
      if (!this.onboardForm.tenantId.trim()) { this.tenantError = 'Tenant ID is required.'; return }
      this.onboarding = true; this.tenantResult = null; this.tenantError = null; this.tenantSuccess = null
      try {
        this.tenantResult = await apiFetch('/api/admin/tenants/onboard', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(this.onboardForm),
        })
        this.tenantSuccess = `Tenant '${this.onboardForm.tenantId}' provisioned successfully.`
        this.showOnboardForm = false
      } catch (e) { this.tenantError = e.message }
      finally { this.onboarding = false }
    },
    async lookupTenant() {
      const id = this.lookupTenantId.trim()
      if (!id) return
      this.tenantStatus = null; this.tenantQuota = null; this.tenantError = null
      try {
        this.tenantStatus = await apiFetch(`/api/admin/tenants/${encodeURIComponent(id)}/status`)
      } catch (e) { this.tenantError = `Status: ${e.message}` }
      try {
        this.tenantQuota = await apiFetch(`/api/admin/tenants/${encodeURIComponent(id)}/quota`)
      } catch { /* quota may not exist yet */ }
    },
    async deprovisionTenant() {
      const id = this.lookupTenantId.trim()
      if (!id) return
      this.tenantError = null; this.tenantSuccess = null
      try {
        await apiFetch(`/api/admin/tenants/${encodeURIComponent(id)}`, { method: 'DELETE' })
        this.tenantSuccess = `Tenant '${id}' deprovisioned.`
        this.tenantStatus = null; this.tenantQuota = null
      } catch (e) { this.tenantError = e.message }
    },
  },
}
</script>
