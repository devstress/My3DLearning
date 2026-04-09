import { describe, it, expect, vi } from 'vitest';
import { ref, nextTick } from 'vue';
import { useDebounce } from '../../composables/useDebounce';

describe('useDebounce', () => {
  it('returns initial value immediately', () => {
    const source = ref('hello');
    const debounced = useDebounce(source, 300);
    expect(debounced.value).toBe('hello');
  });

  it('does not update debounced value immediately on change', async () => {
    vi.useFakeTimers();
    const source = ref('hello');
    const debounced = useDebounce(source, 300);
    source.value = 'world';
    await nextTick();
    // Value hasn't changed yet (debounce hasn't fired)
    expect(debounced.value).toBe('hello');
    vi.useRealTimers();
  });

  it('updates debounced value after delay', async () => {
    vi.useFakeTimers();
    const source = ref('hello');
    const debounced = useDebounce(source, 300);
    source.value = 'world';
    await nextTick(); // flush watcher so setTimeout is scheduled
    vi.advanceTimersByTime(350);
    await nextTick(); // flush reactivity after timeout fires
    expect(debounced.value).toBe('world');
    vi.useRealTimers();
  });
});
