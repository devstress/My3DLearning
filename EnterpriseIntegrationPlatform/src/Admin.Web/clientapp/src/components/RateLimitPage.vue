<template>
  <div id="page-ratelimit">
    <div v-if="ratelimitLoading" class="loading">⏳ Loading…</div>
    <div v-else>
      <div class="card">
        <h3>Rate Limit Configuration</h3>
        <div v-if="ratelimitData.adminApi" class="json-block" id="ratelimit-data">{{ JSON.stringify(ratelimitData, null, 2) }}</div>
        <div v-else class="alert alert-warning">Unable to load rate limit status</div>
      </div>
    </div>
  </div>
</template>

<script>
import { apiFetch } from '../api.js'

export default {
  name: 'RateLimitPage',
  data() {
    return {
      ratelimitData: {},
      ratelimitLoading: true,
    }
  },
  async mounted() {
    await this.loadRateLimit()
  },
  methods: {
    async loadRateLimit() {
      this.ratelimitLoading = true
      try {
        this.ratelimitData = await apiFetch('/api/admin/ratelimit/status') || {}
      } catch {
        this.ratelimitData = {}
      } finally {
        this.ratelimitLoading = false
      }
    },
  },
}
</script>
