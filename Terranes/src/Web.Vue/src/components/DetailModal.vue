<script setup lang="ts">
import { watch, nextTick, onUnmounted, ref } from 'vue';

const props = defineProps<{ title: string; show: boolean }>();
const emit = defineEmits<{ close: [] }>();

const closeButtonRef = ref<HTMLButtonElement | null>(null);

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') {
    emit('close');
  }
}

watch(
  () => props.show,
  (newVal) => {
    if (newVal) {
      document.addEventListener('keydown', onKeydown);
      nextTick(() => {
        closeButtonRef.value?.focus();
      });
    } else {
      document.removeEventListener('keydown', onKeydown);
    }
  },
);

onUnmounted(() => {
  document.removeEventListener('keydown', onKeydown);
});
</script>

<template>
  <div v-if="show" class="modal show d-block" tabindex="-1" role="dialog" aria-modal="true" style="background-color: rgba(0,0,0,0.5);">
    <div class="modal-dialog modal-lg">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title">{{ title }}</h5>
          <button ref="closeButtonRef" type="button" class="btn-close" aria-label="Close modal" @click="$emit('close')"></button>
        </div>
        <div class="modal-body">
          <slot />
        </div>
      </div>
    </div>
  </div>
</template>
