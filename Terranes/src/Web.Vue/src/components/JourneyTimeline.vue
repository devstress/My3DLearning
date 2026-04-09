<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  stages: string[];
  currentStage: string;
  startedUtc: string;
}>();

const currentIndex = computed(() => props.stages.indexOf(props.currentStage));
const startDate = computed(() => new Date(props.startedUtc).toLocaleDateString());
</script>

<template>
  <div class="journey-timeline">
    <h6 class="mb-3">Journey Timeline</h6>
    <div class="timeline-list">
      <div
        v-for="(stage, idx) in stages"
        :key="stage"
        class="timeline-item d-flex mb-3"
        :class="{
          completed: idx < currentIndex,
          active: idx === currentIndex,
          future: idx > currentIndex,
        }"
      >
        <div class="timeline-marker me-3">
          <span
            class="badge rounded-circle d-flex align-items-center justify-content-center"
            :class="{
              'bg-success': idx < currentIndex,
              'bg-primary': idx === currentIndex,
              'bg-light text-muted border': idx > currentIndex,
            }"
            style="width: 28px; height: 28px;"
          >
            <span v-if="idx < currentIndex">✓</span>
            <span v-else>{{ idx + 1 }}</span>
          </span>
        </div>
        <div class="timeline-content">
          <strong :class="{ 'text-muted': idx > currentIndex }">{{ stage }}</strong>
          <div v-if="idx === 0" class="text-muted small">Started {{ startDate }}</div>
          <div v-if="idx === currentIndex && idx > 0" class="text-muted small">In progress</div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.timeline-item + .timeline-item {
  border-left: 2px solid #dee2e6;
  margin-left: 13px;
  padding-left: 24px;
}
.timeline-item:first-child {
  padding-left: 0;
}
.timeline-item.completed + .timeline-item {
  border-left-color: #198754;
}
</style>
