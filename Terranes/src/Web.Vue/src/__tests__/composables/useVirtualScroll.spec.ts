import { describe, it, expect } from 'vitest';
import { ref, nextTick } from 'vue';
import { useVirtualScroll } from '../../composables/useVirtualScroll';

describe('useVirtualScroll', () => {
  it('returns all items when list fits in container', () => {
    const items = ref(['a', 'b', 'c']);
    const { visibleItems, totalHeight } = useVirtualScroll(items, 40, 200);
    expect(visibleItems.value).toEqual(['a', 'b', 'c']);
    expect(totalHeight.value).toBe(120);
  });

  it('returns a window of items for a scrolled list', () => {
    const data = Array.from({ length: 100 }, (_, i) => `item-${i}`);
    const items = ref(data);
    const { visibleItems, onScroll, startIndex, endIndex } = useVirtualScroll(items, 40, 200, 2);

    // Simulate scroll to item 20
    onScroll({ target: { scrollTop: 800 } } as unknown as Event);
    expect(startIndex.value).toBe(18); // 20 - overscan(2)
    expect(endIndex.value).toBe(27); // 20 + ceil(200/40) + 2
    expect(visibleItems.value.length).toBe(9);
    expect(visibleItems.value[0]).toBe('item-18');
  });

  it('computes totalHeight correctly', () => {
    const items = ref(Array.from({ length: 50 }, (_, i) => i));
    const { totalHeight } = useVirtualScroll(items, 30, 300);
    expect(totalHeight.value).toBe(1500);
  });

  it('handles null items', () => {
    const items = ref<string[] | null>(null);
    const { visibleItems, totalHeight } = useVirtualScroll(items, 40, 200);
    expect(visibleItems.value).toEqual([]);
    expect(totalHeight.value).toBe(0);
  });

  it('computes offsetY based on startIndex', () => {
    const data = Array.from({ length: 100 }, (_, i) => `item-${i}`);
    const items = ref(data);
    const { offsetY, onScroll } = useVirtualScroll(items, 40, 200, 2);
    onScroll({ target: { scrollTop: 400 } } as unknown as Event);
    expect(offsetY.value).toBe(320); // startIndex=8 * 40
  });

  it('verifies routes use lazy import for code splitting', async () => {
    // This test verifies that the router configuration uses dynamic imports
    const routerModule = await import('../../router/index');
    const router = routerModule.default;
    const routes = router.getRoutes();
    expect(routes.length).toBeGreaterThan(0);
    for (const route of routes) {
      // Lazy-loaded routes have function components (dynamic import thunks)
      const defaultComponent = route.components?.default;
      expect(typeof defaultComponent).toBe('function');
    }
  });
});
