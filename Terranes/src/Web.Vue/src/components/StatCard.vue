<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';

const props = withDefaults(
  defineProps<{
    label: string;
    value: number;
    color?: string;
    icon?: string;
  }>(),
  { color: 'primary', icon: undefined },
);

const displayValue = ref(0);

function animateTo(target: number) {
  const mql = typeof window !== 'undefined' && window.matchMedia
    ? window.matchMedia('(prefers-reduced-motion: reduce)')
    : null;
  const prefersReducedMotion = mql?.matches ?? false;
  if (prefersReducedMotion || target === 0) {
    displayValue.value = target;
    return;
  }

  const duration = 1000;
  const start = performance.now();
  const from = 0;

  function step(now: number) {
    const elapsed = now - start;
    const progress = Math.min(elapsed / duration, 1);
    displayValue.value = Math.round(from + (target - from) * progress);
    if (progress < 1) {
      requestAnimationFrame(step);
    }
  }

  requestAnimationFrame(step);
}

onMounted(() => animateTo(props.value));
watch(() => props.value, (v) => { displayValue.value = v; });
</script>

<template>
  <div class="card shadow-sm text-center stat-card">
    <div class="card-body">
      <span v-if="icon" class="stat-icon" aria-hidden="true">{{ icon }}</span>
      <h3 :class="`text-${color}`" class="stat-value">{{ displayValue }}</h3>
      <small class="text-muted stat-label">{{ label }}</small>
      <div class="mt-1">
        <slot />
      </div>
    </div>
  </div>
</template>
