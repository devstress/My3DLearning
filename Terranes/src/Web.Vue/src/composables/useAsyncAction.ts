import { ref } from 'vue';
import { useToast } from './useToast';

export function useAsyncAction() {
  const loading = ref(false);
  const { showSuccess, showError } = useToast();

  async function run<T>(
    action: () => Promise<T>,
    options?: { successMessage?: string; errorMessage?: string },
  ): Promise<T | undefined> {
    loading.value = true;
    try {
      const result = await action();
      if (options?.successMessage) {
        showSuccess(options.successMessage);
      }
      return result;
    } catch (err: unknown) {
      const message = options?.errorMessage
        ?? (err instanceof Error ? err.message : 'An unexpected error occurred');
      showError(message);
      return undefined;
    } finally {
      loading.value = false;
    }
  }

  return { loading, run };
}
