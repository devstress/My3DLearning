<script setup lang="ts">
defineProps<{
  modelValue: string;
  placeholder?: string;
}>();

defineEmits<{
  (e: 'update:modelValue', value: string): void;
}>();
</script>

<template>
  <div class="input-group">
    <span class="input-group-text" aria-hidden="true">🔍</span>
    <input
      type="text"
      class="form-control"
      :placeholder="placeholder ?? 'Search...'"
      :value="modelValue"
      aria-label="Search"
      @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
    <button
      v-if="modelValue"
      class="btn btn-outline-secondary"
      type="button"
      aria-label="Clear search"
      @click="$emit('update:modelValue', '')"
    >
      ✕
    </button>
  </div>
</template>
