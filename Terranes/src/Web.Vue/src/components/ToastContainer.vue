<script setup lang="ts">
import { useToast } from '../composables/useToast';

const { toasts, removeToast } = useToast();

function toastBgClass(type: string): string {
  switch (type) {
    case 'success': return 'bg-success text-white';
    case 'error': return 'bg-danger text-white';
    case 'info': return 'bg-info text-white';
    default: return 'bg-secondary text-white';
  }
}
</script>

<template>
  <div class="toast-container position-fixed bottom-0 end-0 p-3" style="z-index: 1090;" aria-live="polite">
    <TransitionGroup name="toast">
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="toast show mb-2"
        :class="toastBgClass(toast.type)"
        role="alert"
        :aria-label="toast.type + ' notification'"
      >
        <div class="d-flex align-items-center">
          <div class="toast-body flex-grow-1">
            {{ toast.message }}
          </div>
          <button
            type="button"
            class="btn-close btn-close-white me-2"
            :aria-label="'Dismiss ' + toast.type + ' notification'"
            @click="removeToast(toast.id)"
          ></button>
        </div>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toast {
  min-width: 280px;
  max-width: 400px;
}

.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s ease;
}

.toast-enter-from {
  opacity: 0;
  transform: translateX(40px);
}

.toast-leave-to {
  opacity: 0;
  transform: translateX(40px);
}

@media (prefers-reduced-motion: reduce) {
  .toast-enter-active,
  .toast-leave-active {
    transition: none;
  }
}
</style>
