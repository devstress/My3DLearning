import { computed, ref, type Ref } from 'vue';

/**
 * Basic virtual scroll composable for large lists.
 * Uses a windowed approach: only renders items within the visible range + overscan.
 */
export function useVirtualScroll<T>(
  items: Ref<T[] | null>,
  itemHeight: number,
  containerHeight: number,
  overscan = 5,
) {
  const scrollTop = ref(0);

  const totalHeight = computed(() => {
    return (items.value?.length ?? 0) * itemHeight;
  });

  const startIndex = computed(() => {
    const idx = Math.floor(scrollTop.value / itemHeight) - overscan;
    return Math.max(0, idx);
  });

  const endIndex = computed(() => {
    const visibleCount = Math.ceil(containerHeight / itemHeight);
    const idx = Math.floor(scrollTop.value / itemHeight) + visibleCount + overscan;
    return Math.min(items.value?.length ?? 0, idx);
  });

  const visibleItems = computed(() => {
    if (!items.value) return [];
    return items.value.slice(startIndex.value, endIndex.value);
  });

  const offsetY = computed(() => {
    return startIndex.value * itemHeight;
  });

  function onScroll(event: Event) {
    const target = event.target as HTMLElement;
    scrollTop.value = target.scrollTop;
  }

  return {
    scrollTop,
    totalHeight,
    startIndex,
    endIndex,
    visibleItems,
    offsetY,
    onScroll,
  };
}
