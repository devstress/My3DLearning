<script setup lang="ts">
withDefaults(defineProps<{
  show: boolean;
  title?: string;
  message?: string;
  confirmText?: string;
  cancelText?: string;
  variant?: 'danger' | 'warning' | 'primary';
}>(), {
  title: 'Are you sure?',
  message: 'This action cannot be undone.',
  confirmText: 'Confirm',
  cancelText: 'Cancel',
  variant: 'danger',
});

defineEmits<{
  (e: 'confirm'): void;
  (e: 'cancel'): void;
}>();
</script>

<template>
  <div v-if="show" class="modal d-block" tabindex="-1" role="dialog" aria-modal="true" :aria-label="title">
    <div class="modal-backdrop show" @click="$emit('cancel')"></div>
    <div class="modal-dialog modal-dialog-centered" role="document">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title">{{ title }}</h5>
          <button type="button" class="btn-close" aria-label="Close" @click="$emit('cancel')"></button>
        </div>
        <div class="modal-body">
          <p>{{ message }}</p>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" @click="$emit('cancel')">{{ cancelText }}</button>
          <button type="button" :class="`btn btn-${variant}`" @click="$emit('confirm')">{{ confirmText }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  z-index: 1040;
}

.modal-dialog {
  z-index: 1050;
  position: relative;
}
</style>
