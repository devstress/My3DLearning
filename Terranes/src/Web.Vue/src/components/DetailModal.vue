<script setup lang="ts">
import { watch, ref, nextTick, onBeforeUnmount } from 'vue';

const props = defineProps<{ title: string; show: boolean }>();
const emit = defineEmits<{ close: [] }>();

const modalRef = ref<HTMLElement | null>(null);

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') {
    emit('close');
    return;
  }
  if (e.key === 'Tab' && modalRef.value) {
    const focusable = modalRef.value.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
    );
    if (focusable.length === 0) return;
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (e.shiftKey) {
      if (document.activeElement === first) {
        e.preventDefault();
        last.focus();
      }
    } else {
      if (document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    }
  }
}

watch(() => props.show, async (isOpen) => {
  if (isOpen) {
    await nextTick();
    modalRef.value?.focus();
    document.addEventListener('keydown', onKeydown);
  } else {
    document.removeEventListener('keydown', onKeydown);
  }
});

onBeforeUnmount(() => {
  document.removeEventListener('keydown', onKeydown);
});
</script>

<template>
  <div
    v-if="show"
    ref="modalRef"
    class="modal show d-block"
    tabindex="-1"
    role="dialog"
    :aria-label="title"
    aria-modal="true"
    style="background-color: rgba(0,0,0,0.5);"
    @click.self="$emit('close')"
  >
    <div class="modal-dialog modal-lg">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title">{{ title }}</h5>
          <button type="button" class="btn-close" aria-label="Close modal" @click="$emit('close')"></button>
        </div>
        <div class="modal-body">
          <slot />
        </div>
      </div>
    </div>
  </div>
</template>
