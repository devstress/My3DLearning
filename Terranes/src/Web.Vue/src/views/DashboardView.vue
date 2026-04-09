<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { RouterLink } from 'vue-router';
import { api } from '../api/client';
import type { BuyerJourney, Notification as AppNotification, HomeModel } from '../types';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import StatusBadge from '../components/StatusBadge.vue';
import StatCard from '../components/StatCard.vue';
import SparklineChart from '../components/SparklineChart.vue';

const DEMO_BUYER_ID = '00000000-0000-0000-0000-000000000001';

const activeJourneys = ref<BuyerJourney[] | null>(null);
const notifications = ref<AppNotification[] | null>(null);
const recentModels = ref<HomeModel[] | null>(null);
const activeJourneyCount = ref(0);
const homeModelCount = ref(0);
const listingCount = ref(0);
const analyticsEventCount = ref(0);

const unreadCount = computed(() =>
  notifications.value?.filter(n => !n.isRead).length ?? 0,
);

const trendJourneys = [5, 8, 12, 9, 15, 20, 18];
const trendModels = [3, 6, 4, 10, 8, 14, 12];
const trendListings = [2, 5, 7, 6, 11, 9, 13];
const trendAnalytics = [10, 25, 18, 30, 22, 35, 42];

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
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>📊 Dashboard</h2>
      <div class="d-flex align-items-center gap-3">
        <span class="position-relative notification-bell" aria-label="Notifications">
          🔔
          <span
            v-if="unreadCount > 0"
            class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger notification-count"
          >
            {{ unreadCount }}
          </span>
        </span>
      </div>
    </div>

    <div class="d-flex flex-wrap gap-2 mb-4">
      <RouterLink to="/journey" class="btn btn-primary quick-action">New Journey</RouterLink>
      <RouterLink to="/home-models" class="btn btn-outline-secondary quick-action">Browse Designs</RouterLink>
    </div>

    <div class="row g-4 mb-4">
      <div class="col-6 col-md-3">
        <StatCard label="Active Journeys" :value="activeJourneyCount" color="primary" icon="🚀">
          <SparklineChart :data="trendJourneys" color="#0d6efd" />
        </StatCard>
      </div>
      <div class="col-6 col-md-3">
        <StatCard label="Home Designs" :value="homeModelCount" color="success" icon="🏡">
          <SparklineChart :data="trendModels" color="#198754" />
        </StatCard>
      </div>
      <div class="col-6 col-md-3">
        <StatCard label="Marketplace Listings" :value="listingCount" color="info" icon="🏬">
          <SparklineChart :data="trendListings" color="#0dcaf0" />
        </StatCard>
      </div>
      <div class="col-6 col-md-3">
        <StatCard label="Analytics Events" :value="analyticsEventCount" color="warning" icon="📈">
          <SparklineChart :data="trendAnalytics" color="#ffc107" />
        </StatCard>
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
          <div class="card-header"><strong>Recent Notifications</strong></div>
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
