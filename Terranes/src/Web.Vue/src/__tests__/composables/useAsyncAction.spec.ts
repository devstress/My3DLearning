import { describe, it, expect, vi, beforeEach } from 'vitest';
import { useAsyncAction } from '../../composables/useAsyncAction';

// Mock the useToast composable
vi.mock('../../composables/useToast', () => {
  const showSuccess = vi.fn();
  const showError = vi.fn();
  const showInfo = vi.fn();
  return {
    useToast: () => ({ showSuccess, showError, showInfo, toasts: { value: [] }, removeToast: vi.fn() }),
  };
});

import { useToast } from '../../composables/useToast';

describe('useAsyncAction', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('sets loading to true during action', async () => {
    const { loading, run } = useAsyncAction();
    let wasLoading = false;
    await run(async () => {
      wasLoading = loading.value;
      return 'result';
    });
    expect(wasLoading).toBe(true);
    expect(loading.value).toBe(false);
  });

  it('returns action result on success', async () => {
    const { run } = useAsyncAction();
    const result = await run(async () => 42);
    expect(result).toBe(42);
  });

  it('shows success toast when successMessage provided', async () => {
    const { run } = useAsyncAction();
    const { showSuccess } = useToast();
    await run(async () => 'ok', { successMessage: 'Done!' });
    expect(showSuccess).toHaveBeenCalledWith('Done!');
  });

  it('returns undefined and shows error toast on failure', async () => {
    const { run } = useAsyncAction();
    const { showError } = useToast();
    const result = await run(async () => { throw new Error('fail'); });
    expect(result).toBeUndefined();
    expect(showError).toHaveBeenCalledWith('fail');
  });

  it('uses custom error message when provided', async () => {
    const { run } = useAsyncAction();
    const { showError } = useToast();
    await run(async () => { throw new Error('fail'); }, { errorMessage: 'Custom error' });
    expect(showError).toHaveBeenCalledWith('Custom error');
  });

  it('resets loading to false even on error', async () => {
    const { loading, run } = useAsyncAction();
    await run(async () => { throw new Error('fail'); });
    expect(loading.value).toBe(false);
  });
});
