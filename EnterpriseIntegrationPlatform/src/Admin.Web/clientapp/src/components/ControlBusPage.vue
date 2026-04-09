<template>
  <div id="page-controlbus">
    <div class="card">
      <h3>Control Bus</h3>
      <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
        Send control commands through the messaging infrastructure.
        Like BizTalk's Control Bus, administrative operations flow through the same
        messaging channels as business messages.
      </p>
      <div class="form-row">
        <div class="form-group" style="flex:1">
          <label>Command Type</label>
          <select v-model="commandType" id="cb-command-type">
            <option value="config.reload">config.reload</option>
            <option value="throttle.update">throttle.update</option>
            <option value="route.enable">route.enable</option>
            <option value="route.disable">route.disable</option>
            <option value="endpoint.start">endpoint.start</option>
            <option value="endpoint.stop">endpoint.stop</option>
            <option value="cache.clear">cache.clear</option>
            <option value="custom">Custom…</option>
          </select>
        </div>
        <div v-if="commandType === 'custom'" class="form-group" style="flex:1">
          <label>Custom Command Type</label>
          <input v-model="customCommandType" id="cb-custom-type" placeholder="e.g. my.command" />
        </div>
      </div>
      <div class="form-group">
        <label>Payload (JSON)</label>
        <textarea v-model="payload" id="cb-payload" rows="4" placeholder='{"key": "value"}'></textarea>
      </div>
      <button class="btn btn-primary" @click="sendCommand" :disabled="sending" id="btn-send-command">
        {{ sending ? '⏳ Sending…' : '📡 Send Command' }}
      </button>
    </div>

    <div v-if="commandResult" class="card" id="cb-result" style="background:var(--surface2)">
      <h3>Command Result</h3>
      <div class="json-block">{{ JSON.stringify(commandResult, null, 2) }}</div>
    </div>
    <div v-if="commandError" class="alert alert-error">{{ commandError }}</div>

    <div class="card">
      <h3>Command History</h3>
      <div v-if="history.length" id="cb-history">
        <table>
          <thead><tr><th>Command</th><th>Status</th><th>Time</th></tr></thead>
          <tbody>
            <tr v-for="(item, i) in history" :key="i">
              <td>{{ item.commandType }}</td>
              <td><span class="badge" :class="item.success ? 'badge-healthy' : 'badge-unhealthy'">{{ item.success ? 'OK' : 'FAIL' }}</span></td>
              <td style="font-size:0.8rem;color:var(--muted)">{{ item.time }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-else style="text-align:center;color:var(--muted);padding:1rem">No commands sent this session</div>
    </div>
  </div>
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'ControlBusPage',
  data() {
    return {
      commandType: 'config.reload',
      customCommandType: '',
      payload: '{}',
      sending: false,
      commandResult: null,
      commandError: null,
      history: [],
    }
  },
  methods: {
    async sendCommand() {
      const type = this.commandType === 'custom' ? this.customCommandType.trim() : this.commandType
      if (!type) { this.commandError = 'Please select or enter a command type.'; return }
      this.sending = true; this.commandResult = null; this.commandError = null
      try {
        let parsedPayload
        try { parsedPayload = JSON.parse(this.payload || '{}') }
        catch { this.commandError = 'Invalid JSON payload.'; this.sending = false; return }

        this.commandResult = await apiFetch('/api/admin/controlbus/send', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ commandType: type, payload: parsedPayload }),
        })
        this.history.unshift({ commandType: type, success: true, time: new Date().toLocaleTimeString() })
      } catch (e) {
        this.commandError = e.message
        this.history.unshift({ commandType: type, success: false, time: new Date().toLocaleTimeString() })
      } finally { this.sending = false }
    },
  },
}
</script>
