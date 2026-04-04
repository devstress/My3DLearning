<template>
  <div id="page-dr">
    <div class="card">
      <h3>Execute DR Drill</h3>
      <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
        Run a disaster recovery drill scenario to validate failover readiness.
      </p>
      <div class="form-row">
        <div class="form-group"><label>Scenario ID</label><input v-model="drForm.scenarioId" id="dr-scenarioId" placeholder="e.g. failover-us-east" /></div>
        <div class="form-group"><label>Scenario Name</label><input v-model="drForm.name" id="dr-name" placeholder="e.g. US East Failover Drill" /></div>
      </div>
      <div class="form-row">
        <div class="form-group"><label>Target Region</label><input v-model="drForm.targetRegion" id="dr-targetRegion" placeholder="e.g. us-west-2" /></div>
        <div class="form-group"><label>Simulate Failure</label>
          <select v-model="drForm.simulateFailure" id="dr-simulateFailure">
            <option value="true">Yes</option>
            <option value="false">No</option>
          </select>
        </div>
      </div>
      <button class="btn btn-danger" @click="runDrDrill" :disabled="drRunning" id="btn-run-drill">
        {{ drRunning ? '⏳ Running…' : '🚨 Run Drill' }}
      </button>
      <div v-if="drResult" class="card" style="margin-top:0.75rem;background:var(--surface2)" id="dr-result">
        <h3>Drill Result</h3>
        <div class="json-block">{{ JSON.stringify(drResult, null, 2) }}</div>
      </div>
      <div v-if="drError" class="alert alert-error" style="margin-top:0.75rem">{{ drError }}</div>
    </div>
    <div class="card">
      <h3>Drill History</h3>
      <button class="btn btn-sm btn-refresh" @click="loadDrHistory" id="btn-load-dr-history">↻ Refresh</button>
      <div v-if="drHistoryLoading" class="loading">⏳ Loading…</div>
      <div v-else id="dr-history">
        <div v-if="drHistory.length" class="json-block">{{ JSON.stringify(drHistory, null, 2) }}</div>
        <div v-else style="color:var(--muted);text-align:center;padding:1rem">No drill history available</div>
      </div>
    </div>
  </div>
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'DrDrillsPage',
  data() {
    return {
      drForm: { scenarioId: '', name: '', targetRegion: '', simulateFailure: 'false' },
      drRunning: false,
      drResult: null,
      drError: null,
      drHistory: [],
      drHistoryLoading: false,
    }
  },
  async mounted() {
    await this.loadDrHistory()
  },
  methods: {
    async runDrDrill() {
      this.drRunning = true
      this.drResult = null
      this.drError = null
      try {
        this.drResult = await apiFetch('/api/admin/dr/drills', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            scenarioId: this.drForm.scenarioId || 'drill-' + Date.now(),
            name: this.drForm.name || 'Ad-hoc DR Drill',
            targetRegion: this.drForm.targetRegion || 'local',
            simulateFailure: this.drForm.simulateFailure === 'true',
          }),
        })
      } catch (e) {
        this.drError = e.message
      } finally {
        this.drRunning = false
      }
    },
    async loadDrHistory() {
      this.drHistoryLoading = true
      try {
        this.drHistory = await apiFetch('/api/admin/dr/drills/history') || []
      } catch {
        this.drHistory = []
      } finally {
        this.drHistoryLoading = false
      }
    },
  },
}
</script>
