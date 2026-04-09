<script setup lang="ts">
defineProps<{ label: string }>();
defineEmits<{ remove: [] }>();
</script>

<template>
  <span class="badge bg-primary d-inline-flex align-items-center me-2 mb-1 filter-chip">
    {{ label }}
    <button
      type="button"
      class="btn-close btn-close-white ms-2"
      style="font-size: 0.6em;"
      aria-label="Remove filter"
      @click="$emit('remove')"
    ></button>
  </span>
</template>
