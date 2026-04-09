<script setup lang="ts">
defineProps<{
  label: string;
  value: string;
}>();

defineEmits<{
  (e: 'remove'): void;
}>();
</script>

<template>
  <span class="badge bg-primary d-inline-flex align-items-center gap-1 filter-chip">
    {{ label }}: {{ value }}
    <button
      type="button"
      class="btn-close btn-close-white filter-chip-remove"
      :aria-label="`Remove ${label} filter`"
      @click="$emit('remove')"
    ></button>
  </span>
</template>

<style scoped>
.filter-chip {
  font-size: 0.85rem;
  padding: 0.35em 0.65em;
}

.filter-chip-remove {
  font-size: 0.6rem;
  padding: 0.15em;
}
</style>
