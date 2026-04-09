<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{
    data: number[];
    color?: string;
    height?: number;
    width?: number;
  }>(),
  { color: '#0d6efd', height: 40, width: 120 },
);

const points = computed(() => {
  if (props.data.length === 0) return '';
  const max = Math.max(...props.data);
  const min = Math.min(...props.data);
  const range = max - min || 1;
  const stepX = props.width / Math.max(props.data.length - 1, 1);
  return props.data
    .map((v, i) => {
      const x = i * stepX;
      const y = props.height - ((v - min) / range) * (props.height - 4) - 2;
      return `${x},${y}`;
    })
    .join(' ');
});
</script>

<template>
  <svg
    :width="width"
    :height="height"
    role="img"
    aria-label="Sparkline chart"
    class="sparkline-chart"
  >
    <polyline
      :points="points"
      fill="none"
      :stroke="color"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
    />
  </svg>
</template>
