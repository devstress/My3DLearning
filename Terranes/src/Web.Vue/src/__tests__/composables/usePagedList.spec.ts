import { describe, it, expect } from 'vitest';
import { ref } from 'vue';
import { usePagedList } from '../../composables/usePagedList';

describe('usePagedList', () => {
  it('returns null pagedItems when source is null', () => {
    const items = ref<string[] | null>(null);
    const { pagedItems } = usePagedList(items, 5);
    expect(pagedItems.value).toBeNull();
  });

  it('returns first page of items', () => {
    const items = ref<number[] | null>([1, 2, 3, 4, 5, 6, 7]);
    const { pagedItems } = usePagedList(items, 3);
    expect(pagedItems.value).toEqual([1, 2, 3]);
  });

  it('calculates total pages correctly', () => {
    const items = ref<number[] | null>([1, 2, 3, 4, 5, 6, 7]);
    const { totalPages } = usePagedList(items, 3);
    expect(totalPages.value).toBe(3);
  });

  it('navigates to next page', () => {
    const items = ref<number[] | null>([1, 2, 3, 4, 5]);
    const { pagedItems, nextPage } = usePagedList(items, 2);
    nextPage();
    expect(pagedItems.value).toEqual([3, 4]);
  });

  it('navigates to previous page', () => {
    const items = ref<number[] | null>([1, 2, 3, 4, 5]);
    const { pagedItems, goToPage, prevPage } = usePagedList(items, 2);
    goToPage(3);
    prevPage();
    expect(pagedItems.value).toEqual([3, 4]);
  });

  it('does not go below page 1', () => {
    const items = ref<number[] | null>([1, 2, 3]);
    const { currentPage, prevPage } = usePagedList(items, 2);
    prevPage();
    expect(currentPage.value).toBe(1);
  });

  it('does not go beyond total pages', () => {
    const items = ref<number[] | null>([1, 2, 3]);
    const { currentPage, totalPages, nextPage } = usePagedList(items, 2);
    nextPage();
    nextPage();
    nextPage();
    expect(currentPage.value).toBe(totalPages.value);
  });

  it('resets page to 1', () => {
    const items = ref<number[] | null>([1, 2, 3, 4, 5]);
    const { currentPage, goToPage, resetPage } = usePagedList(items, 2);
    goToPage(3);
    resetPage();
    expect(currentPage.value).toBe(1);
  });
});
