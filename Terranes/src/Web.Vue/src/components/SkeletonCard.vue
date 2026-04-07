<script setup lang="ts">
withDefaults(defineProps<{
  count?: number;
  columns?: number;
}>(), {
  count: 3,
  columns: 3,
});
</script>

<template>
  <div class="row g-4">
    <div
      v-for="i in count"
      :key="i"
      :class="`col-md-${12 / columns}`"
    >
      <div class="card h-100 shadow-sm placeholder-glow" aria-hidden="true">
        <div class="card-body">
          <h5 class="card-title placeholder col-6"></h5>
          <p class="card-text">
            <span class="placeholder col-8"></span>
            <span class="placeholder col-4"></span>
          </p>
          <div class="d-flex justify-content-between mb-2">
            <span class="placeholder col-3 bg-primary rounded"></span>
            <span class="placeholder col-3 bg-secondary rounded"></span>
          </div>
        </div>
        <div class="card-footer">
          <span class="placeholder col-4 btn btn-primary disabled" aria-hidden="true"></span>
        </div>
      </div>
    </div>
  </div>
</template>
