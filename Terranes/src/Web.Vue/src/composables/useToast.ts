import { ref } from 'vue';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info';
  autoDismiss: boolean;
}

let nextId = 1;

const toasts = ref<Toast[]>([]);

function addToast(message: string, type: Toast['type'], autoDismiss: boolean) {
  const id = nextId++;
  toasts.value.push({ id, message, type, autoDismiss });
  if (autoDismiss) {
    setTimeout(() => removeToast(id), 5000);
  }
}

function removeToast(id: number) {
  toasts.value = toasts.value.filter((t) => t.id !== id);
}

export function useToast() {
  return {
    toasts,
    showSuccess(message: string) {
      addToast(message, 'success', true);
    },
    showError(message: string) {
      addToast(message, 'error', false);
    },
    showInfo(message: string) {
      addToast(message, 'info', true);
    },
    removeToast,
  };
}
