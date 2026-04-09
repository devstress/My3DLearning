import { ref, computed, type Ref } from 'vue';

export function usePagedList<T>(items: Ref<T[] | null>, pageSize = 12) {
  const currentPage = ref(1);

  const totalPages = computed(() => {
    if (!items.value) return 0;
    return Math.max(1, Math.ceil(items.value.length / pageSize));
  });

  const pagedItems = computed(() => {
    if (!items.value) return null;
    const start = (currentPage.value - 1) * pageSize;
    return items.value.slice(start, start + pageSize);
  });

  function goToPage(page: number) {
    if (page >= 1 && page <= totalPages.value) {
      currentPage.value = page;
    }
  }

  function nextPage() {
    goToPage(currentPage.value + 1);
  }

  function prevPage() {
    goToPage(currentPage.value - 1);
  }

  function resetPage() {
    currentPage.value = 1;
  }

  return { currentPage, totalPages, pagedItems, goToPage, nextPage, prevPage, resetPage };
}
