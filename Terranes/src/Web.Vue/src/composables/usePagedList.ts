import { ref, computed, type Ref } from 'vue';

export function usePagedList<T>(items: Ref<T[]>, batchSize = 20) {
  const visibleCount = ref(batchSize);
  const visibleItems = computed(() => items.value.slice(0, visibleCount.value));
  const hasMore = computed(() => visibleCount.value < items.value.length);
  function showMore() { visibleCount.value += batchSize; }
  function resetVisible() { visibleCount.value = batchSize; }
  return { visibleItems, hasMore, showMore, resetVisible };
}
