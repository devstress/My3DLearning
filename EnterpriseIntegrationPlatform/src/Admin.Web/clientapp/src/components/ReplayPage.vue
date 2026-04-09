<template>
  <div id="page-replay">
    <div class="card">
      <h3>Message Replay</h3>
      <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
        Replay previously processed messages back through the pipeline.
        Like BizTalk's tracked message replay, this re-publishes historical messages
        to a target topic for reprocessing.
      </p>
      <div class="form-row">
        <div class="form-group"><label>Correlation ID</label><input v-model="replayForm.correlationId" id="replay-correlationId" placeholder="Optional GUID filter" /></div>
        <div class="form-group"><label>Message Type</label><input v-model="replayForm.messageType" id="replay-messageType" placeholder="Optional type filter" /></div>
      </div>
      <div class="form-row">
        <div class="form-group"><label>From Timestamp</label><input type="datetime-local" v-model="replayForm.fromTimestamp" id="replay-from" /></div>
        <div class="form-group"><label>To Timestamp</label><input type="datetime-local" v-model="replayForm.toTimestamp" id="replay-to" /></div>
      </div>
      <div class="form-row">
        <div class="form-group"><label>Source Topic</label><input v-model="replayForm.sourceTopic" id="replay-source" placeholder="e.g. orders.processed" /></div>
        <div class="form-group"><label>Target Topic</label><input v-model="replayForm.targetTopic" id="replay-target" placeholder="e.g. orders.replay" /></div>
      </div>
      <button class="btn btn-primary" @click="startReplay" :disabled="replaying" id="btn-start-replay">
        {{ replaying ? '⏳ Replaying…' : '🔄 Start Replay' }}
      </button>
    </div>

    <div v-if="replayResult" class="card" id="replay-result" style="background:var(--surface2)">
      <h3>Replay Result</h3>
      <div class="card-grid">
        <div class="stat-card">
          <div class="label">Replayed</div>
          <div class="value" style="color:var(--green)">{{ replayResult.replayedCount }}</div>
        </div>
        <div class="stat-card">
          <div class="label">Skipped</div>
          <div class="value" style="color:var(--muted)">{{ replayResult.skippedCount }}</div>
        </div>
        <div class="stat-card">
          <div class="label">Failed</div>
          <div class="value" style="color:var(--red)">{{ replayResult.failedCount }}</div>
        </div>
        <div class="stat-card">
          <div class="label">Duration</div>
          <div class="value" style="font-size:0.9rem">{{ formatReplayDuration }}</div>
        </div>
      </div>
    </div>
    <div v-if="replayError" class="alert alert-error">{{ replayError }}</div>

    <div class="card">
      <h3>Replay History</h3>
      <div v-if="history.length" id="replay-history">
        <table>
          <thead><tr><th>Type Filter</th><th>Replayed</th><th>Skipped</th><th>Failed</th><th>Time</th></tr></thead>
          <tbody>
            <tr v-for="(item, i) in history" :key="i">
              <td>{{ item.messageType || '(all)' }}</td>
              <td style="color:var(--green)"><strong>{{ item.replayed }}</strong></td>
              <td style="color:var(--muted)">{{ item.skipped }}</td>
              <td style="color:var(--red)">{{ item.failed }}</td>
              <td style="font-size:0.8rem;color:var(--muted)">{{ item.time }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-else style="text-align:center;color:var(--muted);padding:1rem">No replays performed this session</div>
    </div>
  </div>
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'ReplayPage',
  data() {
    return {
      replayForm: { correlationId: '', messageType: '', fromTimestamp: '', toTimestamp: '', sourceTopic: '', targetTopic: '' },
      replaying: false,
      replayResult: null,
      replayError: null,
      history: [],
    }
  },
  computed: {
    formatReplayDuration() {
      if (!this.replayResult?.startedAt || !this.replayResult?.completedAt) return '—'
      const start = new Date(this.replayResult.startedAt)
      const end = new Date(this.replayResult.completedAt)
      const ms = end - start
      return ms < 1000 ? `${ms}ms` : `${(ms / 1000).toFixed(2)}s`
    },
  },
  methods: {
    async startReplay() {
      this.replaying = true
      this.replayResult = null
      this.replayError = null
      try {
        const payload = {}
        if (this.replayForm.correlationId) payload.correlationId = this.replayForm.correlationId
        if (this.replayForm.messageType) payload.messageType = this.replayForm.messageType
        if (this.replayForm.fromTimestamp) payload.fromTimestamp = new Date(this.replayForm.fromTimestamp).toISOString()
        if (this.replayForm.toTimestamp) payload.toTimestamp = new Date(this.replayForm.toTimestamp).toISOString()
        if (this.replayForm.sourceTopic) payload.sourceTopic = this.replayForm.sourceTopic
        if (this.replayForm.targetTopic) payload.targetTopic = this.replayForm.targetTopic
        this.replayResult = await apiFetch('/api/admin/replay', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        })
        this.history.unshift({
          messageType: this.replayForm.messageType,
          replayed: this.replayResult?.replayedCount ?? 0,
          skipped: this.replayResult?.skippedCount ?? 0,
          failed: this.replayResult?.failedCount ?? 0,
          time: new Date().toLocaleTimeString(),
        })
      } catch (e) {
        this.replayError = e.message
      } finally {
        this.replaying = false
      }
    },
  },
}
</script>
