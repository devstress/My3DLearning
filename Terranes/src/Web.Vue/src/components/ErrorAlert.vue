<script setup lang="ts">
defineProps<{ message: string | null }>();
</script>

<template>
  <div v-if="message" class="alert alert-danger">{{ message }}</div>
</template>
