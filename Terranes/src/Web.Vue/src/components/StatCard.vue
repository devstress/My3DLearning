<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';

const props = withDefaults(defineProps<{
  value: number;
  label: string;
  icon?: string;
  color?: 'primary' | 'success' | 'info' | 'warning' | 'danger';
  animate?: boolean;
}>(), {
  icon: '📊',
  color: 'primary',
  animate: true,
});

const displayValue = ref(0);
let animFrame: ReturnType<typeof requestAnimationFrame> | null = null;

const isTestEnv = typeof navigator !== 'undefined' && navigator.userAgent.includes('jsdom');

function animateCount(target: number) {
  if (!props.animate || isTestEnv) {
    displayValue.value = target;
    return;
  }
  const start = displayValue.value;
  const diff = target - start;
  if (diff === 0) return;
  const duration = 800;
  const startTime = performance.now();

  function tick(now: number) {
    const elapsed = now - startTime;
    const progress = Math.min(elapsed / duration, 1);
    const eased = 1 - Math.pow(1 - progress, 3);
    displayValue.value = Math.round(start + diff * eased);
    if (progress < 1) {
      animFrame = requestAnimationFrame(tick);
    }
  }

  if (animFrame) cancelAnimationFrame(animFrame);
  animFrame = requestAnimationFrame(tick);
}

onMounted(() => {
  animateCount(props.value);
});

watch(() => props.value, (newVal) => {
  animateCount(newVal);
});
</script>

<template>
  <div class="card shadow-sm stat-card card-hover-lift">
    <div class="card-body d-flex align-items-center gap-3">
      <div class="stat-icon" :class="`text-${color}`" aria-hidden="true">
        <span class="stat-icon-emoji">{{ icon }}</span>
      </div>
      <div>
        <h3 class="mb-0 stat-value" :class="`text-${color}`">{{ displayValue.toLocaleString() }}</h3>
        <small class="text-muted stat-label">{{ label }}</small>
      </div>
    </div>
  </div>
</template>

<style scoped>
.stat-card {
  transition: transform 0.2s ease;
}

.stat-icon-emoji {
  font-size: 2rem;
}

.stat-value {
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.stat-label {
  font-size: 0.85rem;
}
</style>
