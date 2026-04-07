import { describe, it, expect, vi, beforeEach } from 'vitest';
import { useToast } from '../../composables/useToast';

describe('useToast', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    // Clear any leftover toasts from previous tests
    const { toasts, removeToast } = useToast();
    toasts.value.forEach((t) => removeToast(t.id));
  });

  it('starts with no toasts', () => {
    const { toasts } = useToast();
    expect(toasts.value).toHaveLength(0);
  });

  it('showSuccess adds a success toast', () => {
    const { toasts, showSuccess } = useToast();
    showSuccess('Test success');
    expect(toasts.value).toHaveLength(1);
    expect(toasts.value[0].message).toBe('Test success');
    expect(toasts.value[0].type).toBe('success');
    expect(toasts.value[0].autoDismiss).toBe(true);
  });

  it('showError adds an error toast that does not auto-dismiss', () => {
    const { toasts, showError } = useToast();
    showError('Test error');
    expect(toasts.value).toHaveLength(1);
    expect(toasts.value[0].type).toBe('error');
    expect(toasts.value[0].autoDismiss).toBe(false);
  });

  it('showInfo adds an info toast', () => {
    const { toasts, showInfo } = useToast();
    showInfo('Test info');
    expect(toasts.value).toHaveLength(1);
    expect(toasts.value[0].type).toBe('info');
    expect(toasts.value[0].autoDismiss).toBe(true);
  });

  it('removeToast removes a toast by id', () => {
    const { toasts, showSuccess, removeToast } = useToast();
    showSuccess('First');
    showSuccess('Second');
    expect(toasts.value).toHaveLength(2);
    const firstId = toasts.value[0].id;
    removeToast(firstId);
    expect(toasts.value).toHaveLength(1);
    expect(toasts.value[0].message).toBe('Second');
  });

  it('success toast auto-dismisses after 5 seconds', () => {
    const { toasts, showSuccess } = useToast();
    showSuccess('Auto-dismiss');
    expect(toasts.value).toHaveLength(1);
    vi.advanceTimersByTime(5000);
    expect(toasts.value).toHaveLength(0);
  });

  it('error toast does not auto-dismiss', () => {
    const { toasts, showError } = useToast();
    showError('Persistent error');
    expect(toasts.value).toHaveLength(1);
    vi.advanceTimersByTime(10000);
    expect(toasts.value).toHaveLength(1);
  });
});
