<script setup lang="ts">
export interface TimelineEvent {
  id: string;
  stage: string;
  timestamp: string;
  description: string;
}

defineProps<{
  events: TimelineEvent[];
}>();
</script>

<template>
  <div class="journey-timeline" role="list" aria-label="Journey timeline">
    <div
      v-for="event in events"
      :key="event.id"
      class="timeline-item"
      role="listitem"
    >
      <div class="timeline-marker" aria-hidden="true"></div>
      <div class="timeline-content">
        <div class="d-flex justify-content-between align-items-center">
          <strong class="timeline-stage">{{ event.stage }}</strong>
          <small class="text-muted">{{ new Date(event.timestamp).toLocaleString() }}</small>
        </div>
        <p class="text-muted small mb-0">{{ event.description }}</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.journey-timeline {
  position: relative;
  padding-left: 1.5rem;
}

.journey-timeline::before {
  content: '';
  position: absolute;
  left: 0.5rem;
  top: 0;
  bottom: 0;
  width: 2px;
  background: var(--bs-border-color, #dee2e6);
}

.timeline-item {
  position: relative;
  padding-bottom: 1rem;
  padding-left: 1rem;
}

.timeline-marker {
  position: absolute;
  left: -1.15rem;
  top: 0.35rem;
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 50%;
  background: var(--bs-primary, #0d6efd);
  border: 2px solid var(--bs-body-bg, #fff);
}

.timeline-item:last-child {
  padding-bottom: 0;
}
</style>
