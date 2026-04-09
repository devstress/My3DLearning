import { ref } from 'vue';

const isDark = ref(false);
let initialized = false;

function applyTheme(dark: boolean) {
  isDark.value = dark;
  if (typeof document !== 'undefined') {
    document.documentElement.setAttribute('data-bs-theme', dark ? 'dark' : 'light');
  }
}

function init() {
  if (initialized) return;
  initialized = true;
  if (typeof localStorage !== 'undefined') {
    const stored = localStorage.getItem('terranes-theme');
    if (stored) {
      applyTheme(stored === 'dark');
      return;
    }
  }
  if (typeof window !== 'undefined' && window.matchMedia) {
    const mq = window.matchMedia('(prefers-color-scheme: dark)');
    applyTheme(mq.matches);
    mq.addEventListener('change', (e) => applyTheme(e.matches));
  }
}

export function useTheme() {
  init();
  function toggleTheme() {
    const newVal = !isDark.value;
    applyTheme(newVal);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('terranes-theme', newVal ? 'dark' : 'light');
    }
  }
  return { isDark, toggleTheme };
}

/** Test-only reset */
export function _resetTheme() {
  initialized = false;
  isDark.value = false;
}
