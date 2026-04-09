<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  stages: string[];
  currentStage: string;
}>();

const currentIndex = computed(() => props.stages.indexOf(props.currentStage));
</script>

<template>
  <div class="step-indicator d-flex align-items-center justify-content-between" role="progressbar">
    <template v-for="(stage, idx) in stages" :key="stage">
      <div class="step-item text-center" :class="{
        completed: idx < currentIndex,
        active: idx === currentIndex,
        future: idx > currentIndex,
      }">
        <div class="step-circle mx-auto mb-1" :class="{
          'bg-success text-white': idx < currentIndex,
          'bg-primary text-white': idx === currentIndex,
          'bg-light text-muted border': idx > currentIndex,
        }">
          <span v-if="idx < currentIndex">✓</span>
          <span v-else>{{ idx + 1 }}</span>
        </div>
        <small class="step-label d-none d-md-block">{{ stage }}</small>
      </div>
      <div v-if="idx < stages.length - 1" class="step-line flex-grow-1 mx-1" :class="{
        'bg-success': idx < currentIndex,
        'bg-secondary': idx >= currentIndex,
      }"></div>
    </template>
  </div>
</template>

<style scoped>
.step-circle {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.8rem;
  font-weight: bold;
}
.step-line {
  height: 3px;
  border-radius: 2px;
  opacity: 0.6;
}
.step-label {
  font-size: 0.7rem;
  max-width: 80px;
  word-wrap: break-word;
}
.step-item {
  flex-shrink: 0;
}
</style>
