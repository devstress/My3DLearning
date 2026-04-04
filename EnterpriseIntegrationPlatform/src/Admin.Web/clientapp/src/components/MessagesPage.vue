<template>
  <div id="page-messages">
    <div class="card">
      <h3>Message Inspector</h3>
      <div class="form-row">
        <div class="form-group" style="flex:2">
          <label>Search by Message ID, Correlation ID, or Business Key</label>
          <input v-model="messageQuery" id="message-query" placeholder="Enter ID or key…" @keydown.enter="searchMessages" />
        </div>
        <div class="form-group" style="flex:0 0 auto;display:flex;align-items:flex-end">
          <button class="btn btn-primary" @click="searchMessages" id="btn-search-messages">🔍 Search</button>
        </div>
      </div>
      <div v-if="messageLoading" class="loading">⏳ Searching…</div>
      <div v-if="messageResults" class="card" style="margin-top:0.75rem;background:var(--surface2)" id="message-results">
        <h3>Results</h3>
        <div class="json-block">{{ JSON.stringify(messageResults, null, 2) }}</div>
      </div>
      <div v-if="messageError" class="alert alert-error" style="margin-top:0.75rem">{{ messageError }}</div>
    </div>
  </div>
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'MessagesPage',
  data() {
    return {
      messageQuery: '',
      messageResults: null,
      messageLoading: false,
      messageError: null,
    }
  },
  methods: {
    async searchMessages() {
      const q = this.messageQuery.trim()
      if (!q) return
      this.messageLoading = true
      this.messageResults = null
      this.messageError = null
      try {
        const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
        if (guidRegex.test(q)) {
          try {
            const result = await apiFetch(`/api/admin/messages/${q}`)
            if (result) { this.messageResults = result; return }
          } catch { /* fall through to correlation ID */ }
          const results = await apiFetch(`/api/admin/messages/correlation/${q}`)
          this.messageResults = results
        } else {
          this.messageResults = await apiFetch(`/api/admin/events/business/${encodeURIComponent(q)}`)
        }
      } catch (e) {
        this.messageError = e.message
      } finally {
        this.messageLoading = false
      }
    },
  },
}
</script>
