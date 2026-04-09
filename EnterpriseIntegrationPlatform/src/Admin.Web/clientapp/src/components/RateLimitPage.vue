<template>
  <div id="page-ratelimit">
    <div v-if="ratelimitLoading" class="loading">⏳ Loading…</div>
    <div v-else>
      <div class="card-grid" id="ratelimit-stats">
        <div class="stat-card">
          <div class="label">Rate Limit (req/min)</div>
          <div class="value">{{ ratelimitData.adminApi?.permitLimit ?? '—' }}</div>
        </div>
        <div class="stat-card">
          <div class="label">Window</div>
          <div class="value" style="font-size:0.9rem">{{ ratelimitData.adminApi?.window ?? '—' }}</div>
        </div>
        <div class="stat-card">
          <div class="label">Queue Limit</div>
          <div class="value">{{ ratelimitData.adminApi?.queueLimit ?? 0 }}</div>
        </div>
        <div class="stat-card">
          <div class="label">Rejection Status</div>
          <div class="value" style="font-size:0.9rem">{{ ratelimitData.adminApi?.rejectionStatusCode ?? 429 }}</div>
        </div>
      </div>

      <div class="card">
        <h3>Rate Limit Configuration</h3>
        <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
          The Admin API uses a fixed-window rate limiter partitioned by API key.
          Each API key gets its own independent rate window.
        </p>
        <div v-if="ratelimitData.adminApi" id="ratelimit-data">
          <table>
            <thead><tr><th>Setting</th><th>Value</th><th>Description</th></tr></thead>
            <tbody>
              <tr>
                <td><strong>Permit Limit</strong></td>
                <td>{{ ratelimitData.adminApi.permitLimit }}</td>
                <td style="color:var(--muted);font-size:0.85rem">Maximum requests per window</td>
              </tr>
              <tr>
                <td><strong>Window</strong></td>
                <td>{{ ratelimitData.adminApi.window }}</td>
                <td style="color:var(--muted);font-size:0.85rem">Fixed window duration</td>
              </tr>
              <tr>
                <td><strong>Queue Limit</strong></td>
                <td>{{ ratelimitData.adminApi.queueLimit }}</td>
                <td style="color:var(--muted);font-size:0.85rem">Requests queued when limit hit (0 = reject immediately)</td>
              </tr>
              <tr>
                <td><strong>Partition Key</strong></td>
                <td>{{ ratelimitData.adminApi.partitionKey || 'X-Api-Key header' }}</td>
                <td style="color:var(--muted);font-size:0.85rem">How requests are grouped</td>
              </tr>
              <tr>
                <td><strong>Rejection Code</strong></td>
                <td>{{ ratelimitData.adminApi.rejectionStatusCode }}</td>
                <td style="color:var(--muted);font-size:0.85rem">HTTP status when rate limited</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-else class="alert alert-warning">Unable to load rate limit status</div>
        <button class="btn btn-sm btn-refresh" @click="loadRateLimit" style="margin-top:0.75rem" id="btn-refresh-ratelimit">↻ Refresh</button>
      </div>

      <div v-if="ratelimitData.gatewayApi" class="card">
        <h3>Gateway API Rate Limits</h3>
        <table>
          <thead><tr><th>Setting</th><th>Value</th></tr></thead>
          <tbody>
            <tr v-for="(val, key) in ratelimitData.gatewayApi" :key="key">
              <td><strong>{{ key }}</strong></td>
              <td>{{ val }}</td>
            </tr>
          </tbody>
        </table>
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
