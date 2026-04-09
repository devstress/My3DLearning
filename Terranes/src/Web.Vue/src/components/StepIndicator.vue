<script setup lang="ts">
defineProps<{
  steps: string[];
  currentStep: string;
}>();

function stepIndex(steps: string[], step: string): number {
  return steps.indexOf(step);
}
</script>

<template>
  <div class="step-indicator d-flex align-items-center" role="group" aria-label="Journey progress steps">
    <template v-for="(step, i) in steps" :key="step">
      <div
        class="step-item text-center"
        :class="{
          'step-completed': stepIndex(steps, currentStep) > i,
          'step-active': currentStep === step,
          'step-pending': stepIndex(steps, currentStep) < i,
        }"
      >
        <div class="step-circle" :aria-label="`Step ${i + 1}: ${step}`">
          <span v-if="stepIndex(steps, currentStep) > i" aria-hidden="true">✓</span>
          <span v-else>{{ i + 1 }}</span>
        </div>
        <div class="step-label">{{ step }}</div>
      </div>
      <div
        v-if="i < steps.length - 1"
        class="step-connector"
        :class="{ 'step-connector-active': stepIndex(steps, currentStep) > i }"
      ></div>
    </template>
  </div>
</template>

<style scoped>
.step-indicator {
  overflow-x: auto;
  padding: 0.5rem 0;
}

.step-item {
  flex-shrink: 0;
  min-width: 5rem;
}

.step-circle {
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 0.25rem;
  font-size: 0.8rem;
  font-weight: bold;
  border: 2px solid var(--bs-border-color, #dee2e6);
  color: var(--bs-secondary-color, #6c757d);
  background: var(--bs-body-bg, #fff);
}

.step-completed .step-circle {
  background: var(--bs-success, #198754);
  border-color: var(--bs-success, #198754);
  color: #fff;
}

.step-active .step-circle {
  background: var(--bs-primary, #0d6efd);
  border-color: var(--bs-primary, #0d6efd);
  color: #fff;
}

.step-label {
  font-size: 0.7rem;
  color: var(--bs-secondary-color, #6c757d);
  white-space: nowrap;
}

.step-active .step-label {
  font-weight: bold;
  color: var(--bs-primary, #0d6efd);
}

.step-completed .step-label {
  color: var(--bs-success, #198754);
}

.step-connector {
  flex: 1;
  height: 2px;
  background: var(--bs-border-color, #dee2e6);
  min-width: 1rem;
  margin-bottom: 1rem;
}

.step-connector-active {
  background: var(--bs-success, #198754);
}
</style>
