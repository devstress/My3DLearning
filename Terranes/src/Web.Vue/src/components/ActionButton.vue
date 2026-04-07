<script setup lang="ts">
withDefaults(defineProps<{
  loading?: boolean;
  disabled?: boolean;
  variant?: string;
  size?: string;
  loadingText?: string;
}>(), {
  loading: false,
  disabled: false,
  variant: 'primary',
  size: '',
  loadingText: 'Working...',
});

defineEmits<{ click: [] }>();
</script>

<template>
  <button
    type="button"
    class="btn"
    :class="[`btn-${variant}`, size ? `btn-${size}` : '']"
    :disabled="loading || disabled"
    :aria-busy="loading"
    @click="$emit('click')"
  >
    <span v-if="loading" class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
    <template v-if="loading">{{ loadingText }}</template>
    <template v-else><slot /></template>
  </button>
</template>
