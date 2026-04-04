<template>
  <div id="page-dlq">
    <div class="card">
      <h3>DLQ Resubmission</h3>
      <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
        Resubmit dead-lettered messages back to their original processing pipeline.
        All filters are optional — omitting all fields resubmits all DLQ messages.
      </p>
      <div class="form-row">
        <div class="form-group"><label>Correlation ID</label><input v-model="dlqForm.correlationId" id="dlq-correlationId" placeholder="Optional GUID" /></div>
        <div class="form-group"><label>Message Type</label><input v-model="dlqForm.messageType" id="dlq-messageType" placeholder="Optional" /></div>
      </div>
      <div class="form-row">
        <div class="form-group"><label>From Timestamp</label><input type="datetime-local" v-model="dlqForm.fromTimestamp" /></div>
        <div class="form-group"><label>To Timestamp</label><input type="datetime-local" v-model="dlqForm.toTimestamp" /></div>
      </div>
      <button class="btn btn-primary" @click="resubmitDlq" :disabled="dlqSubmitting" id="btn-resubmit-dlq">
        {{ dlqSubmitting ? '⏳ Resubmitting…' : '🔄 Resubmit' }}
      </button>
      <div v-if="dlqResult" class="card" style="margin-top:0.75rem;background:var(--surface2)" id="dlq-result">
        <h3>Resubmission Result</h3>
        <div class="json-block">{{ JSON.stringify(dlqResult, null, 2) }}</div>
      </div>
      <div v-if="dlqError" class="alert alert-error" style="margin-top:0.75rem">{{ dlqError }}</div>
    </div>
  </div>
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'DlqPage',
  data() {
    return {
      dlqForm: { correlationId: '', messageType: '', fromTimestamp: '', toTimestamp: '' },
      dlqSubmitting: false,
      dlqResult: null,
      dlqError: null,
    }
  },
  methods: {
    async resubmitDlq() {
      this.dlqSubmitting = true
      this.dlqResult = null
      this.dlqError = null
      try {
        const payload = {}
        if (this.dlqForm.correlationId) payload.correlationId = this.dlqForm.correlationId
        if (this.dlqForm.messageType) payload.messageType = this.dlqForm.messageType
        if (this.dlqForm.fromTimestamp) payload.fromTimestamp = new Date(this.dlqForm.fromTimestamp).toISOString()
        if (this.dlqForm.toTimestamp) payload.toTimestamp = new Date(this.dlqForm.toTimestamp).toISOString()
        this.dlqResult = await apiFetch('/api/admin/dlq/resubmit', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        })
      } catch (e) {
        this.dlqError = e.message
      } finally {
        this.dlqSubmitting = false
      }
    },
  },
}
</script>
