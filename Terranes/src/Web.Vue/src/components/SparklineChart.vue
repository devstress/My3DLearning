<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(defineProps<{
  data: number[];
  width?: number;
  height?: number;
  color?: string;
  fillOpacity?: number;
}>(), {
  width: 200,
  height: 50,
  color: '#0d6efd',
  fillOpacity: 0.15,
});

const viewBox = computed(() => `0 0 ${props.width} ${props.height}`);

const points = computed(() => {
  if (props.data.length < 2) return '';
  const max = Math.max(...props.data, 1);
  const min = Math.min(...props.data, 0);
  const range = max - min || 1;
  const xStep = props.width / (props.data.length - 1);

  return props.data
    .map((val, i) => {
      const x = i * xStep;
      const y = props.height - ((val - min) / range) * (props.height * 0.8) - props.height * 0.1;
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    })
    .join(' ');
});

const fillPoints = computed(() => {
  if (!points.value) return '';
  return `0,${props.height} ${points.value} ${props.width},${props.height}`;
});
</script>

<template>
  <svg
    :viewBox="viewBox"
    :width="width"
    :height="height"
    class="sparkline-chart"
    role="img"
    aria-label="Sparkline chart"
  >
    <polygon
      v-if="fillPoints"
      :points="fillPoints"
      :fill="color"
      :fill-opacity="fillOpacity"
    />
    <polyline
      v-if="points"
      :points="points"
      fill="none"
      :stroke="color"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
    />
  </svg>
</template>

<style scoped>
.sparkline-chart {
  display: block;
}
</style>
