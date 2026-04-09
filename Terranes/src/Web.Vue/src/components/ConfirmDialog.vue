<script setup lang="ts">
withDefaults(defineProps<{
  show: boolean;
  title: string;
  message: string;
  confirmText?: string;
  confirmVariant?: string;
}>(), {
  confirmText: 'Confirm',
  confirmVariant: 'primary',
});

defineEmits<{ confirm: []; cancel: [] }>();
</script>

<template>
  <div v-if="show" class="modal d-block" tabindex="-1" role="dialog" aria-modal="true">
    <div class="modal-backdrop fade show" @click="$emit('cancel')"></div>
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
          <button type="button" class="btn btn-secondary" @click="$emit('cancel')">Cancel</button>
          <button type="button" :class="`btn btn-${confirmVariant}`" @click="$emit('confirm')">
            {{ confirmText }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1040;
}
.modal-dialog {
  z-index: 1050;
  position: relative;
}
</style>
