<template>
  <div id="page-test-messages">
    <div class="card">
      <h3>Test Message Generator</h3>
      <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
        Publish synthetic test messages through the pipeline to verify health.
        Messages are tagged with test metadata so downstream processors can identify them.
      </p>
      <div class="form-row">
        <div class="form-group" style="flex:2">
          <label>Target Topic</label>
          <input v-model="targetTopic" id="test-target-topic" placeholder="e.g. orders.created" />
        </div>
        <div class="form-group" style="flex:0 0 auto;display:flex;align-items:flex-end">
          <button class="btn btn-primary" @click="generateTestMessage" :disabled="generating" id="btn-generate-test">
            {{ generating ? '⏳ Sending…' : '🧪 Send Test Message' }}
          </button>
        </div>
      </div>
      <div class="form-group">
        <label><input type="checkbox" v-model="useCustomPayload" /> Custom JSON Payload</label>
      </div>
      <div v-if="useCustomPayload" class="form-group">
        <label>Payload (JSON)</label>
        <textarea v-model="customPayload" id="test-custom-payload" rows="4"
                  placeholder='{"orderId": "TEST-001", "amount": 99.99}'></textarea>
      </div>
    </div>

    <div v-if="testResult" class="card" id="test-result" style="background:var(--surface2)">
      <h3>Result</h3>
      <div class="json-block">{{ JSON.stringify(testResult, null, 2) }}</div>
    </div>
    <div v-if="testError" class="alert alert-error">{{ testError }}</div>

    <div class="card">
      <h3>Recent Test Messages</h3>
      <div v-if="history.length" id="test-history">
        <div v-for="(item, i) in history" :key="i" class="test-history-item">
          <span class="badge" :class="item.success ? 'badge-healthy' : 'badge-unhealthy'">
            {{ item.success ? 'OK' : 'FAIL' }}
          </span>
          <span style="margin-left:0.5rem">{{ item.topic }}</span>
          <span style="margin-left:auto;color:var(--muted);font-size:0.8rem">{{ item.time }}</span>
        </div>
      </div>
      <div v-else style="text-align:center;color:var(--muted);padding:1rem">No test messages sent yet this session</div>
    </div>
  </div>
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'TestMessagesPage',
  data() {
    return {
      targetTopic: '',
      useCustomPayload: false,
      customPayload: '',
      generating: false,
      testResult: null,
      testError: null,
      history: [],
    }
  },
  methods: {
    async generateTestMessage() {
      const topic = this.targetTopic.trim()
      if (!topic) { this.testError = 'Please enter a target topic.'; return }
      this.generating = true
      this.testResult = null
      this.testError = null
      try {
        let result
        if (this.useCustomPayload && this.customPayload.trim()) {
          result = await apiFetch('/api/admin/test-messages/custom', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ payload: this.customPayload.trim(), targetTopic: topic }),
          })
        } else {
          result = await apiFetch('/api/admin/test-messages', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ targetTopic: topic }),
          })
        }
        this.testResult = result
        this.history.unshift({ topic, success: true, time: new Date().toLocaleTimeString() })
      } catch (e) {
        this.testError = e.message
        this.history.unshift({ topic, success: false, time: new Date().toLocaleTimeString() })
      } finally {
        this.generating = false
      }
    },
  },
}
</script>

<style scoped>
.test-history-item {
  display: flex;
  align-items: center;
  padding: 0.5rem 0;
  border-bottom: 1px solid var(--border);
  font-size: 0.85rem;
}
</style>
