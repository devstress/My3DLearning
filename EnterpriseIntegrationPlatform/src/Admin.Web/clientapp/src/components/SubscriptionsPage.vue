<template>
  <div id="page-subscriptions">
    <div class="card">
      <h3>Active Subscriptions</h3>
      <p style="font-size:0.85rem;color:var(--muted);margin-bottom:0.75rem">
        View all active message subscriptions across broker transports.
        Like BizTalk's Subscription Viewer, this shows which consumers are listening to which topics.
      </p>
      <div style="margin-bottom:0.75rem">
        <button class="btn btn-sm btn-refresh" @click="loadSubscriptions" id="btn-refresh-subs">↻ Refresh</button>
        <select v-model="brokerFilter" style="margin-left:0.5rem;padding:0.3rem;background:var(--surface2);color:var(--text);border:1px solid var(--border);border-radius:0.4rem;font-size:0.8rem" @change="loadSubscriptions" id="sub-broker-filter">
          <option value="">All Brokers</option>
          <option value="NatsJetStream">NATS JetStream</option>
          <option value="Kafka">Kafka</option>
          <option value="Pulsar">Pulsar</option>
          <option value="Postgres">PostgreSQL</option>
        </select>
      </div>
      <div v-if="subsLoading" class="loading">⏳ Loading subscriptions…</div>
      <table v-else id="subs-table">
        <thead>
          <tr><th>Topic</th><th>Consumer Group</th><th>Broker</th><th>Status</th><th>Created</th></tr>
        </thead>
        <tbody>
          <tr v-for="s in filteredSubscriptions" :key="s.topic + s.consumerGroup">
            <td>{{ s.topic }}</td>
            <td>{{ s.consumerGroup || '—' }}</td>
            <td><span class="badge badge-enabled">{{ s.brokerType || '—' }}</span></td>
            <td><span class="badge" :class="s.isActive ? 'badge-healthy' : 'badge-disabled'">{{ s.isActive ? 'Active' : 'Inactive' }}</span></td>
            <td style="font-size:0.8rem;color:var(--muted)">{{ formatDate(s.createdAt) }}</td>
          </tr>
          <tr v-if="!filteredSubscriptions.length"><td colspan="5" style="text-align:center;color:var(--muted)">No active subscriptions</td></tr>
        </tbody>
      </table>
      <div v-if="subsError" class="alert alert-error" style="margin-top:0.75rem">{{ subsError }}</div>
    </div>

    <div class="card-grid">
      <div class="stat-card">
        <div class="label">Total Subscriptions</div>
        <div class="value">{{ subscriptions.length }}</div>
      </div>
      <div class="stat-card">
        <div class="label">Active</div>
        <div class="value" style="color:var(--green)">{{ subscriptions.filter(s => s.isActive).length }}</div>
      </div>
      <div class="stat-card">
        <div class="label">Brokers</div>
        <div class="value">{{ uniqueBrokers.length }}</div>
      </div>
      <div class="stat-card">
        <div class="label">Topics</div>
        <div class="value">{{ uniqueTopics.length }}</div>
      </div>
    </div>
  </div>
</template>

<script>
import { apiFetch, formatDate } from '../api.js'

export default {
  name: 'SubscriptionsPage',
  data() {
    return {
      subscriptions: [],
      subsLoading: false,
      subsError: null,
      brokerFilter: '',
    }
  },
  computed: {
    filteredSubscriptions() {
      if (!this.brokerFilter) return this.subscriptions
      return this.subscriptions.filter(s => s.brokerType === this.brokerFilter)
    },
    uniqueBrokers() {
      return [...new Set(this.subscriptions.map(s => s.brokerType).filter(Boolean))]
    },
    uniqueTopics() {
      return [...new Set(this.subscriptions.map(s => s.topic).filter(Boolean))]
    },
  },
  async mounted() { await this.loadSubscriptions() },
  methods: {
    formatDate,
    async loadSubscriptions() {
      this.subsLoading = true; this.subsError = null
      try {
        this.subscriptions = await apiFetch('/api/admin/subscriptions') || []
      } catch (e) { this.subsError = e.message; this.subscriptions = [] }
      finally { this.subsLoading = false }
    },
  },
}
</script>
