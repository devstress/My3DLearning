<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { api } from '../api/client';
import type { BuyerJourney, Notification as AppNotification, HomeModel } from '../types';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import StatusBadge from '../components/StatusBadge.vue';
import StatCard from '../components/StatCard.vue';
import SparklineChart from '../components/SparklineChart.vue';
import QuoteSummary from '../components/QuoteSummary.vue';

const DEMO_BUYER_ID = '00000000-0000-0000-0000-000000000001';

const activeJourneys = ref<BuyerJourney[] | null>(null);
const notifications = ref<AppNotification[] | null>(null);
const recentModels = ref<HomeModel[] | null>(null);
const activeJourneyCount = ref(0);
const homeModelCount = ref(0);
const listingCount = ref(0);
const analyticsEventCount = ref(0);

const unreadCount = computed(() =>
  notifications.value?.filter((n) => !n.isRead).length ?? 0,
);

const completedJourneyCount = computed(() =>
  activeJourneys.value?.filter((j) => j.currentStage === 'Completed').length ?? 0,
);

const pendingQuoteCount = computed(() =>
  activeJourneys.value?.filter((j) => j.currentStage === 'QuoteRequested').length ?? 0,
);

// Mock sparkline data for dashboard visualization
const activityData = ref([3, 7, 4, 8, 5, 12, 9, 15, 11, 18, 14, 20]);

onMounted(async () => {
  const [journeys, notifs, models, listings, analytics] = await Promise.all([
    api.getBuyerJourneys(DEMO_BUYER_ID),
    api.getNotifications(DEMO_BUYER_ID),
    api.getHomeModels(),
    api.getListings(),
    api.getAnalyticsCount(),
  ]);

  activeJourneys.value = journeys;
  activeJourneyCount.value = journeys.length;
  notifications.value = notifs;
  recentModels.value = models;
  homeModelCount.value = models.length;
  listingCount.value = listings.length;
  analyticsEventCount.value = analytics;
});
</script>

<template>
  <div class="container">
    <h2 class="mb-4">📊 Dashboard</h2>

    <div class="row g-4 mb-4">
      <div class="col-6 col-md-3">
        <StatCard :value="activeJourneyCount" label="Active Journeys" icon="🚀" color="primary" />
      </div>
      <div class="col-6 col-md-3">
        <StatCard :value="homeModelCount" label="Home Designs" icon="🏡" color="success" />
      </div>
      <div class="col-6 col-md-3">
        <StatCard :value="listingCount" label="Marketplace Listings" icon="🏬" color="info" />
      </div>
      <div class="col-6 col-md-3">
        <StatCard :value="analyticsEventCount" label="Analytics Events" icon="📈" color="warning" />
      </div>
    </div>

    <div class="row g-4 mb-4">
      <div class="col-12 col-md-8">
        <div class="card shadow-sm">
          <div class="card-header d-flex justify-content-between align-items-center">
            <strong>📈 Activity Trend</strong>
          </div>
          <div class="card-body text-center">
            <SparklineChart :data="activityData" :width="600" :height="80" color="#0d6efd" />
          </div>
        </div>
      </div>
      <div class="col-12 col-md-4">
        <QuoteSummary
          :total-journeys="activeJourneyCount"
          :completed-journeys="completedJourneyCount"
          :pending-quotes="pendingQuoteCount"
        />
      </div>
    </div>

    <div class="card shadow-sm mb-4">
      <div class="card-body">
        <h5 class="mb-3">⚡ Quick Actions</h5>
        <div class="d-flex flex-wrap gap-2">
          <RouterLink to="/journey" class="btn btn-primary">🚀 Start Journey</RouterLink>
          <RouterLink to="/home-models" class="btn btn-outline-success">🏡 Browse Designs</RouterLink>
          <RouterLink to="/land" class="btn btn-outline-info">🗺️ Find Land</RouterLink>
          <RouterLink to="/marketplace" class="btn btn-outline-warning">🏬 Marketplace</RouterLink>
        </div>
      </div>
    </div>

    <div class="row g-4">
      <div class="col-12 col-md-6">
        <div class="card shadow-sm h-100">
          <div class="card-header"><strong>Active Buyer Journeys</strong></div>
          <div class="card-body">
            <LoadingSpinner v-if="activeJourneys === null" />
            <p v-else-if="activeJourneys.length === 0" class="text-muted">No active journeys.</p>
            <div v-else class="list-group list-group-flush">
              <div
                v-for="j in activeJourneys.slice(0, 10)"
                :key="j.id"
                class="list-group-item d-flex justify-content-between align-items-center px-0"
              >
                <div>
                  <strong>{{ j.id.substring(0, 8) }}...</strong>
                  <br /><small class="text-muted">Started {{ new Date(j.startedUtc).toLocaleString() }}</small>
                </div>
                <StatusBadge :status="j.currentStage" />
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="col-12 col-md-6">
        <div class="card shadow-sm h-100">
          <div class="card-header d-flex justify-content-between align-items-center">
            <strong>🔔 Recent Notifications</strong>
            <span v-if="unreadCount > 0" class="badge bg-danger rounded-pill notification-bell">{{ unreadCount }}</span>
          </div>
          <div class="card-body">
            <LoadingSpinner v-if="notifications === null" />
            <p v-else-if="notifications.length === 0" class="text-muted">No notifications.</p>
            <div v-else class="list-group list-group-flush">
              <div
                v-for="n in notifications.slice(0, 10)"
                :key="n.id"
                class="list-group-item px-0"
                :class="{ 'fw-bold': !n.isRead }"
              >
                <div class="d-flex justify-content-between">
                  <span>{{ n.title }}</span>
                  <span class="badge" :class="!n.isRead ? 'bg-danger' : 'bg-secondary'">
                    {{ n.isRead ? 'Read' : 'Unread' }}
                  </span>
                </div>
                <small class="text-muted">{{ n.message }}</small>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="row g-4 mt-2">
      <div class="col-12">
        <div class="card shadow-sm">
          <div class="card-header"><strong>Recent Home Designs</strong></div>
          <div class="card-body">
            <div v-if="recentModels && recentModels.length > 0" class="row g-3">
              <div class="col-6 col-md-3" v-for="model in recentModels.slice(0, 4)" :key="model.id">
                <div class="card h-100">
                  <div class="card-body">
                    <h6>{{ model.name }}</h6>
                    <small>{{ model.bedrooms }} bed, {{ model.bathrooms }} bath</small><br />
                    <small class="text-muted">{{ model.floorAreaSqm.toFixed(0) }} m²</small>
                  </div>
                </div>
              </div>
            </div>
            <p v-else class="text-muted">No home designs yet.</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
