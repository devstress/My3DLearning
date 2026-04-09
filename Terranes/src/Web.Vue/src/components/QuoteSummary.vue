<script setup lang="ts">
defineProps<{
  totalJourneys: number;
  completedJourneys: number;
  pendingQuotes: number;
}>();
</script>

<template>
  <div class="card shadow-sm h-100">
    <div class="card-header"><strong>📋 Quote Summary</strong></div>
    <div class="card-body">
      <div class="d-flex justify-content-between mb-2">
        <span class="text-muted">Total Journeys</span>
        <strong>{{ totalJourneys }}</strong>
      </div>
      <div class="d-flex justify-content-between mb-2">
        <span class="text-muted">Completed</span>
        <strong class="text-success">{{ completedJourneys }}</strong>
      </div>
      <div class="d-flex justify-content-between mb-2">
        <span class="text-muted">Pending Quotes</span>
        <strong class="text-warning">{{ pendingQuotes }}</strong>
      </div>
      <div class="progress mt-3" style="height: 8px;">
        <div
          class="progress-bar bg-success"
          :style="{ width: totalJourneys > 0 ? (completedJourneys / totalJourneys * 100) + '%' : '0%' }"
          :aria-valuenow="completedJourneys"
          :aria-valuemax="totalJourneys"
          aria-label="Completion progress"
        ></div>
      </div>
      <small class="text-muted">
        {{ totalJourneys > 0 ? Math.round(completedJourneys / totalJourneys * 100) : 0 }}% completion rate
      </small>
    </div>
  </div>
</template>
