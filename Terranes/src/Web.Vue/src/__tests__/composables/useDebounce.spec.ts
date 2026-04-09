import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { ref, nextTick } from 'vue';
import { useDebounce } from '../../composables/useDebounce';

describe('useDebounce', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('returns initial value immediately', () => {
    const source = ref('hello');
    const debounced = useDebounce(source, 300);
    expect(debounced.value).toBe('hello');
  });

  it('does not update before delay', async () => {
    const source = ref('a');
    const debounced = useDebounce(source, 300);
    source.value = 'b';
    await nextTick();
    vi.advanceTimersByTime(200);
    expect(debounced.value).toBe('a');
  });

  it('updates after delay', async () => {
    const source = ref('a');
    const debounced = useDebounce(source, 300);
    source.value = 'b';
    await nextTick();
    vi.advanceTimersByTime(300);
    expect(debounced.value).toBe('b');
  });

  it('resets timer on rapid changes', async () => {
    const source = ref('a');
    const debounced = useDebounce(source, 300);
    source.value = 'b';
    await nextTick();
    vi.advanceTimersByTime(200);
    source.value = 'c';
    await nextTick();
    vi.advanceTimersByTime(200);
    expect(debounced.value).toBe('a');
    vi.advanceTimersByTime(100);
    expect(debounced.value).toBe('c');
  });

  it('works with number values', async () => {
    const source = ref(0);
    const debounced = useDebounce(source, 100);
    source.value = 42;
    await nextTick();
    vi.advanceTimersByTime(100);
    expect(debounced.value).toBe(42);
  });
});
