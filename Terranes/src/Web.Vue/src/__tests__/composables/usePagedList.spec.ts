import { describe, it, expect } from 'vitest';
import { ref } from 'vue';
import { usePagedList } from '../../composables/usePagedList';

describe('usePagedList', () => {
  it('shows only the first batch of items', () => {
    const items = ref(Array.from({ length: 50 }, (_, i) => i));
    const { visibleItems, hasMore } = usePagedList(items, 20);
    expect(visibleItems.value).toHaveLength(20);
    expect(hasMore.value).toBe(true);
  });

  it('showMore reveals the next batch', () => {
    const items = ref(Array.from({ length: 50 }, (_, i) => i));
    const { visibleItems, showMore, hasMore } = usePagedList(items, 20);
    showMore();
    expect(visibleItems.value).toHaveLength(40);
    expect(hasMore.value).toBe(true);
    showMore();
    expect(visibleItems.value).toHaveLength(50);
    expect(hasMore.value).toBe(false);
  });

  it('resetVisible resets to initial batch size', () => {
    const items = ref(Array.from({ length: 50 }, (_, i) => i));
    const { visibleItems, showMore, resetVisible } = usePagedList(items, 20);
    showMore();
    expect(visibleItems.value).toHaveLength(40);
    resetVisible();
    expect(visibleItems.value).toHaveLength(20);
  });
});
