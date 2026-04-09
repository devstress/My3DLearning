<script setup lang="ts">
withDefaults(defineProps<{ message?: string }>(), {
  message: 'No results found. Try adjusting your filters.',
});
</script>

<template>
  <div class="text-center py-5 empty-state">
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="64"
      height="64"
      fill="currentColor"
      class="text-muted mb-3"
      viewBox="0 0 16 16"
      aria-hidden="true"
    >
      <path d="M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85zm-5.242.656a5.5 5.5 0 1 1 0-11 5.5 5.5 0 0 1 0 11z"/>
    </svg>
    <p class="text-muted">{{ message }}</p>
  </div>
</template>
