import { describe, it, expect, beforeEach } from 'vitest';
import { useTheme, _resetTheme } from '../../composables/useTheme';

describe('useTheme', () => {
  beforeEach(() => {
    _resetTheme();
    localStorage.clear();
    document.documentElement.removeAttribute('data-bs-theme');
  });

  it('isDark defaults to false', () => {
    const { isDark } = useTheme();
    expect(isDark.value).toBe(false);
  });

  it('toggleTheme toggles isDark', () => {
    const { isDark, toggleTheme } = useTheme();
    expect(isDark.value).toBe(false);
    toggleTheme();
    expect(isDark.value).toBe(true);
    toggleTheme();
    expect(isDark.value).toBe(false);
  });

  it('toggleTheme saves to localStorage', () => {
    const { toggleTheme } = useTheme();
    toggleTheme();
    expect(localStorage.getItem('terranes-theme')).toBe('dark');
    toggleTheme();
    expect(localStorage.getItem('terranes-theme')).toBe('light');
  });

  it('init reads from localStorage', () => {
    localStorage.setItem('terranes-theme', 'dark');
    const { isDark } = useTheme();
    expect(isDark.value).toBe(true);
  });

  it('toggleTheme sets data-bs-theme attribute on document', () => {
    const { toggleTheme } = useTheme();
    toggleTheme();
    expect(document.documentElement.getAttribute('data-bs-theme')).toBe('dark');
    toggleTheme();
    expect(document.documentElement.getAttribute('data-bs-theme')).toBe('light');
  });
});
