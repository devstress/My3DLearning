import { ref, watch, type Ref } from 'vue';

export function useDebounce<T>(source: Ref<T>, delayMs = 300): Ref<T> {
  const debounced = ref(source.value) as Ref<T>;
  let timeout: ReturnType<typeof setTimeout> | null = null;

  watch(source, (val) => {
    if (timeout) clearTimeout(timeout);
    timeout = setTimeout(() => {
      debounced.value = val;
    }, delayMs);
  });

  return debounced;
}
