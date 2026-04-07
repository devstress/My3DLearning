<script setup lang="ts">
withDefaults(defineProps<{ message?: string }>(), { message: 'Loading...' });
</script>

<template>
  <p><em>{{ message }}</em></p>
</template>
