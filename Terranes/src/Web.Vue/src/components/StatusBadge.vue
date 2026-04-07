<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{ status: string; colorMap?: Record<string, string> }>();

const defaultColors: Record<string, string> = {
  Active: 'bg-success',
  Draft: 'bg-warning',
  UnderOffer: 'bg-info',
  Sold: 'bg-danger',
  Withdrawn: 'bg-secondary',
  Browsing: 'bg-info',
  DesignSelected: 'bg-primary',
  PlacedOnLand: 'bg-primary',
  Customising: 'bg-warning text-dark',
  QuoteRequested: 'bg-warning text-dark',
  QuoteReceived: 'bg-success',
  Referred: 'bg-success',
  Completed: 'bg-success',
  Abandoned: 'bg-danger',
  Occupied: 'bg-success',
  Vacant: 'bg-warning',
};

const badgeClass = computed(() => {
  const map = props.colorMap ?? defaultColors;
  return map[props.status] ?? 'bg-secondary';
});
</script>

<template>
  <span class="badge" :class="badgeClass">{{ status }}</span>
</template>
