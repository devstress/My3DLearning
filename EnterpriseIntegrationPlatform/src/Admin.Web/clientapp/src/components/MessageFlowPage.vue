<template>
  <div id="page-message-flow">
    <div class="card">
      <h3>Message Flow Timeline</h3>
      <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
        Track message lifecycle events as they flow through the integration pipeline.
        Search by Correlation ID or Business Key to see the full processing timeline.
      </p>
      <div class="form-row">
        <div class="form-group" style="flex:1">
          <label>Search Type</label>
          <select v-model="searchType" id="flow-search-type">
            <option value="correlation">Correlation ID</option>
            <option value="business">Business Key</option>
          </select>
        </div>
        <div class="form-group" style="flex:3">
          <label>{{ searchType === 'correlation' ? 'Correlation ID (GUID)' : 'Business Key' }}</label>
          <input v-model="searchQuery" id="flow-search-query"
                 :placeholder="searchType === 'correlation' ? 'e.g. 3fa85f64-5717-4562-b3fc-2c963f66afa6' : 'e.g. ORDER-1234'"
                 @keydown.enter="searchFlow" />
        </div>
        <div class="form-group" style="flex:0 0 auto;display:flex;align-items:flex-end">
          <button class="btn btn-primary" @click="searchFlow" :disabled="flowLoading" id="btn-search-flow">
            {{ flowLoading ? '⏳ Loading…' : '🔍 Track' }}
          </button>
        </div>
      </div>
    </div>

    <div v-if="flowError" class="alert alert-error">{{ flowError }}</div>

    <div v-if="flowResult" id="flow-result">
      <!-- Summary Card -->
      <div class="card-grid">
        <div class="stat-card">
          <div class="label">Status</div>
          <div class="value" :class="latestStatusClass">{{ flowResult.latestStatus || '—' }}</div>
        </div>
        <div class="stat-card">
          <div class="label">Current Stage</div>
          <div class="value" style="font-size:1rem">{{ flowResult.latestStage || '—' }}</div>
        </div>
        <div class="stat-card">
          <div class="label">Events</div>
          <div class="value">{{ flowResult.events ? flowResult.events.length : 0 }}</div>
        </div>
        <div class="stat-card">
          <div class="label">Query</div>
          <div class="value" style="font-size:0.85rem">{{ flowResult.query || '—' }}</div>
        </div>
      </div>

      <!-- AI Analysis Summary -->
      <div v-if="flowResult.summary" class="card" id="flow-summary">
        <h3>{{ flowResult.ollamaAvailable ? '🤖 AI Trace Analysis' : '⚠️ Trace Analysis' }}</h3>
        <div :class="flowResult.ollamaAvailable ? '' : 'alert alert-warning'" style="font-size:0.9rem;white-space:pre-wrap;">{{ flowResult.summary }}</div>
      </div>

      <!-- Timeline -->
      <div v-if="flowResult.events && flowResult.events.length" class="card" id="flow-timeline">
        <h3>📊 Lifecycle Timeline</h3>
        <div class="timeline">
          <div v-for="(event, index) in flowResult.events" :key="index"
               class="timeline-item" :class="'timeline-' + statusClass(event.status)"
               @click="toggleEvent(index)">
            <div class="timeline-marker" :class="'marker-' + statusClass(event.status)"></div>
            <div class="timeline-content">
              <div class="timeline-header">
                <span class="badge" :class="'badge-' + statusClass(event.status)">{{ statusLabel(event.status) }}</span>
                <span class="timeline-stage">{{ event.stage }}</span>
                <span class="timeline-time">{{ formatDate(event.timestamp) }}</span>
              </div>
              <div v-if="event.details" class="timeline-details">{{ event.details }}</div>
              <div v-if="expandedEvents.includes(index)" class="timeline-expanded">
                <table class="timeline-detail-table">
                  <tbody>
                    <tr><td class="detail-label">Message ID</td><td>{{ event.messageId }}</td></tr>
                    <tr><td class="detail-label">Correlation ID</td><td>{{ event.correlationId }}</td></tr>
                    <tr><td class="detail-label">Message Type</td><td>{{ event.messageType || '—' }}</td></tr>
                    <tr><td class="detail-label">Source</td><td>{{ event.source || '—' }}</td></tr>
                    <tr v-if="event.businessKey"><td class="detail-label">Business Key</td><td>{{ event.businessKey }}</td></tr>
                    <tr v-if="event.traceId"><td class="detail-label">Trace ID</td><td class="mono">{{ event.traceId }}</td></tr>
                    <tr v-if="event.spanId"><td class="detail-label">Span ID</td><td class="mono">{{ event.spanId }}</td></tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Not Found -->
      <div v-if="flowResult.found === false" class="card">
        <div style="text-align:center;padding:1rem;color:var(--muted)">
          No events found for this query. The message may not have been tracked yet.
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { apiFetch, formatDate } from '../api.js'

export default {
  name: 'MessageFlowPage',
  data() {
    return {
      searchType: 'correlation',
      searchQuery: '',
      flowResult: null,
      flowLoading: false,
      flowError: null,
      expandedEvents: [],
    }
  },
  computed: {
    latestStatusClass() {
      if (!this.flowResult?.latestStatus) return ''
      return this.statusClass(this.flowResult.latestStatus).toLowerCase()
    },
  },
  methods: {
    formatDate,
    statusClass(status) {
      if (status == null) return 'pending'
      const s = typeof status === 'string' ? status.toLowerCase() : String(status)
      if (s === 'delivered' || s === '2') return 'delivered'
      if (s === 'failed' || s === '3') return 'failed'
      if (s === 'deadlettered' || s === '4') return 'deadlettered'
      if (s === 'retrying' || s === '5') return 'retrying'
      if (s === 'inflight' || s === '1') return 'inflight'
      return 'pending'
    },
    statusLabel(status) {
      if (status == null) return 'Pending'
      const s = typeof status === 'string' ? status : String(status)
      const labels = {
        'Pending': 'Pending', '0': 'Pending',
        'InFlight': 'In-Flight', '1': 'In-Flight',
        'Delivered': 'Delivered', '2': 'Delivered',
        'Failed': 'Failed', '3': 'Failed',
        'DeadLettered': 'Dead-Lettered', '4': 'Dead-Lettered',
        'Retrying': 'Retrying', '5': 'Retrying',
      }
      return labels[s] || s
    },
    toggleEvent(index) {
      const pos = this.expandedEvents.indexOf(index)
      if (pos >= 0) {
        this.expandedEvents.splice(pos, 1)
      } else {
        this.expandedEvents.push(index)
      }
    },
    async searchFlow() {
      const q = this.searchQuery.trim()
      if (!q) return
      this.flowLoading = true
      this.flowResult = null
      this.flowError = null
      this.expandedEvents = []
      try {
        if (this.searchType === 'correlation') {
          const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
          if (!guidRegex.test(q)) {
            this.flowError = 'Please enter a valid GUID for Correlation ID search.'
            return
          }
          this.flowResult = await apiFetch(`/api/admin/flow/correlation/${q}`)
        } else {
          this.flowResult = await apiFetch(`/api/admin/flow/business/${encodeURIComponent(q)}`)
        }
      } catch (e) {
        this.flowError = 'Failed to load message flow: ' + e.message
      } finally {
        this.flowLoading = false
      }
    },
  },
}
</script>

<style scoped>
.timeline {
  position: relative;
  padding-left: 2rem;
}

.timeline::before {
  content: '';
  position: absolute;
  left: 0.75rem;
  top: 0;
  bottom: 0;
  width: 2px;
  background: var(--border);
}

.timeline-item {
  position: relative;
  margin-bottom: 1rem;
  cursor: pointer;
  padding: 0.75rem;
  border-radius: 0.5rem;
  transition: background 0.15s;
}

.timeline-item:hover {
  background: var(--surface2);
}

.timeline-marker {
  position: absolute;
  left: -1.35rem;
  top: 1rem;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 2px solid var(--border);
  background: var(--surface);
}

.marker-delivered { background: var(--green); border-color: var(--green); }
.marker-failed { background: var(--red); border-color: var(--red); }
.marker-deadlettered { background: var(--orange); border-color: var(--orange); }
.marker-retrying { background: var(--yellow); border-color: var(--yellow); }
.marker-inflight { background: var(--accent); border-color: var(--accent); }
.marker-pending { background: var(--muted); border-color: var(--muted); }

.timeline-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.timeline-stage {
  font-weight: 600;
  font-size: 0.9rem;
}

.timeline-time {
  font-size: 0.8rem;
  color: var(--muted);
  margin-left: auto;
}

.timeline-details {
  font-size: 0.85rem;
  color: var(--muted);
  margin-top: 0.25rem;
}

.timeline-expanded {
  margin-top: 0.5rem;
  padding: 0.5rem;
  background: var(--bg);
  border: 1px solid var(--border);
  border-radius: 0.4rem;
}

.timeline-detail-table {
  width: 100%;
  font-size: 0.8rem;
}

.timeline-detail-table td {
  padding: 0.25rem 0.5rem;
  border-bottom: 1px solid var(--border);
}

.detail-label {
  color: var(--muted);
  font-weight: 600;
  width: 120px;
}

.mono {
  font-family: 'Fira Code', monospace;
  font-size: 0.75rem;
}

/* Status-specific value colors */
.value.delivered { color: var(--green); }
.value.failed { color: var(--red); }
.value.deadlettered { color: var(--orange); }
.value.retrying { color: var(--yellow); }
.value.inflight { color: var(--accent); }
.value.pending { color: var(--muted); }

/* Additional badge types */
.badge-delivered { background: var(--green); color: #000; }
.badge-failed { background: var(--red); color: #fff; }
.badge-deadlettered { background: var(--orange); color: #000; }
.badge-retrying { background: var(--yellow); color: #000; }
.badge-inflight { background: var(--accent); color: #000; }
.badge-pending { background: var(--muted); color: #000; }
</style>
