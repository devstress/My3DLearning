<template>
  <div id="page-events">
    <div class="card">
      <h3>Event Store Browser</h3>
      <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
        Browse event-sourced aggregate streams. Like BizTalk's Business Activity Monitoring (BAM),
        this provides visibility into the ordered stream of business events for each aggregate.
      </p>
      <div class="form-row">
        <div class="form-group" style="flex:2">
          <label>Stream ID (Aggregate ID)</label>
          <input v-model="streamId" id="event-stream-id" placeholder="e.g. order-12345" @keydown.enter="loadStream" />
        </div>
        <div class="form-group" style="flex:0 0 auto;display:flex;align-items:flex-end">
          <button class="btn btn-primary" @click="loadStream" :disabled="eventsLoading" id="btn-load-stream">
            {{ eventsLoading ? '⏳ Loading…' : '📖 Load Stream' }}
          </button>
        </div>
      </div>
    </div>

    <div v-if="eventsError" class="alert alert-error">{{ eventsError }}</div>

    <div v-if="events.length" id="event-results">
      <div class="card-grid">
        <div class="stat-card">
          <div class="label">Stream</div>
          <div class="value" style="font-size:0.9rem">{{ streamId }}</div>
        </div>
        <div class="stat-card">
          <div class="label">Events</div>
          <div class="value">{{ events.length }}</div>
        </div>
        <div class="stat-card">
          <div class="label">Latest Version</div>
          <div class="value">{{ latestVersion }}</div>
        </div>
        <div class="stat-card">
          <div class="label">First Event</div>
          <div class="value" style="font-size:0.85rem">{{ formatDate(events[0]?.timestamp) }}</div>
        </div>
      </div>

      <div class="card">
        <h3>Event Timeline</h3>
        <div class="timeline">
          <div v-for="(event, index) in events" :key="index"
               class="timeline-item" @click="toggleEvent(index)">
            <div class="timeline-marker" :class="'marker-v' + (index % 3)"></div>
            <div class="timeline-content">
              <div class="timeline-header">
                <span class="badge badge-enabled">v{{ event.version }}</span>
                <span class="timeline-stage">{{ event.eventType }}</span>
                <span class="timeline-time">{{ formatDate(event.timestamp) }}</span>
              </div>
              <div v-if="expandedEvents.includes(index)" class="timeline-expanded">
                <div class="json-block" style="font-size:0.8rem">{{ JSON.stringify(event.data, null, 2) }}</div>
                <div v-if="event.metadata" style="margin-top:0.5rem">
                  <span style="font-size:0.75rem;color:var(--muted)">Metadata:</span>
                  <div class="json-block" style="font-size:0.75rem">{{ JSON.stringify(event.metadata, null, 2) }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div v-else-if="searched && !eventsLoading" class="card" style="text-align:center;color:var(--muted);padding:2rem">
      No events found for stream "{{ streamId }}".
    </div>
  </div>
</template>

<script>
import { apiFetch, formatDate } from '../api.js'

export default {
  name: 'EventStorePage',
  data() {
    return {
      streamId: '',
      events: [],
      eventsLoading: false,
      eventsError: null,
      expandedEvents: [],
      searched: false,
    }
  },
  computed: {
    latestVersion() {
      if (!this.events.length) return '—'
      return Math.max(...this.events.map(e => e.version || 0))
    },
  },
  methods: {
    formatDate,
    toggleEvent(index) {
      const pos = this.expandedEvents.indexOf(index)
      if (pos >= 0) { this.expandedEvents.splice(pos, 1) }
      else { this.expandedEvents.push(index) }
    },
    async loadStream() {
      const id = this.streamId.trim()
      if (!id) return
      this.eventsLoading = true; this.eventsError = null; this.events = []; this.expandedEvents = []; this.searched = true
      try {
        this.events = await apiFetch(`/api/admin/eventstore/stream/${encodeURIComponent(id)}`) || []
      } catch (e) { this.eventsError = e.message }
      finally { this.eventsLoading = false }
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
.timeline-item:hover { background: var(--surface2); }
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
.marker-v0 { background: var(--accent); border-color: var(--accent); }
.marker-v1 { background: var(--green); border-color: var(--green); }
.marker-v2 { background: var(--yellow); border-color: var(--yellow); }
.timeline-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.timeline-stage { font-weight: 600; font-size: 0.9rem; }
.timeline-time { font-size: 0.8rem; color: var(--muted); margin-left: auto; }
.timeline-expanded {
  margin-top: 0.5rem;
  padding: 0.5rem;
  background: var(--bg);
  border: 1px solid var(--border);
  border-radius: 0.4rem;
}
</style>
