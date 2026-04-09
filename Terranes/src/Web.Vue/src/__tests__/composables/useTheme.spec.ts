import { describe, it, expect, beforeEach, vi } from 'vitest';
import { readFileSync } from 'fs';
import { resolve } from 'path';

// Reset module state between tests
let useTheme: typeof import('../../composables/useTheme').useTheme;

describe('useTheme', () => {
  beforeEach(async () => {
    // Clear localStorage
    localStorage.clear();
    // Remove data-bs-theme attribute
    document.documentElement.removeAttribute('data-bs-theme');
    // Re-import module to reset singleton state
    vi.resetModules();
    const mod = await import('../../composables/useTheme');
    useTheme = mod.useTheme;
  });

  it('defaults to light theme when no stored preference', () => {
    const { theme } = useTheme();
    expect(theme.value).toBe('light');
  });

  it('sets data-bs-theme attribute on document', () => {
    useTheme();
    expect(document.documentElement.getAttribute('data-bs-theme')).toBe('light');
  });

  it('toggleTheme switches between light and dark', () => {
    const { theme, toggleTheme } = useTheme();
    expect(theme.value).toBe('light');

    toggleTheme();
    expect(theme.value).toBe('dark');
    expect(document.documentElement.getAttribute('data-bs-theme')).toBe('dark');

    toggleTheme();
    expect(theme.value).toBe('light');
    expect(document.documentElement.getAttribute('data-bs-theme')).toBe('light');
  });

  it('persists theme preference in localStorage', () => {
    const { toggleTheme } = useTheme();
    toggleTheme();
    expect(localStorage.getItem('terranes-theme')).toBe('dark');
    toggleTheme();
    expect(localStorage.getItem('terranes-theme')).toBe('light');
  });

  it('reads stored preference from localStorage', async () => {
    localStorage.setItem('terranes-theme', 'dark');
    // Re-import to pick up localStorage
    vi.resetModules();
    const mod = await import('../../composables/useTheme');
    const { theme } = mod.useTheme();
    expect(theme.value).toBe('dark');
    expect(document.documentElement.getAttribute('data-bs-theme')).toBe('dark');
  });

  it('setTheme explicitly sets and persists the theme', () => {
    const { theme, setTheme } = useTheme();
    setTheme('dark');
    expect(theme.value).toBe('dark');
    expect(localStorage.getItem('terranes-theme')).toBe('dark');
    setTheme('light');
    expect(theme.value).toBe('light');
    expect(localStorage.getItem('terranes-theme')).toBe('light');
  });
});

describe('Dark Mode CSS', () => {
  it('style.css has dark-mode sidebar gradient', () => {
    const css = readFileSync(resolve(__dirname, '../../style.css'), 'utf-8');
    expect(css).toContain('[data-bs-theme="dark"] .sidebar');
    expect(css).toContain('var(--bs-body-bg)');
    expect(css).toContain('var(--bs-border-color)');
  });

  it('style.css has theme toggle button styles', () => {
    const css = readFileSync(resolve(__dirname, '../../style.css'), 'utf-8');
    expect(css).toContain('.theme-toggle-btn');
  });
});
