import { ref } from 'vue';

type Theme = 'light' | 'dark';

const STORAGE_KEY = 'terranes-theme';

const theme = ref<Theme>('light');
const isInitialised = ref(false);

function applyTheme(t: Theme) {
  document.documentElement.setAttribute('data-bs-theme', t);
}

function detectSystemTheme(): Theme {
  if (typeof window !== 'undefined' && typeof window.matchMedia === 'function' && window.matchMedia('(prefers-color-scheme: dark)').matches) {
    return 'dark';
  }
  return 'light';
}

export function useTheme() {
  if (!isInitialised.value) {
    isInitialised.value = true;
    const stored = typeof localStorage !== 'undefined' ? localStorage.getItem(STORAGE_KEY) : null;
    if (stored === 'light' || stored === 'dark') {
      theme.value = stored;
    } else {
      theme.value = detectSystemTheme();
    }
    applyTheme(theme.value);

    if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (!stored) {
          theme.value = e.matches ? 'dark' : 'light';
          applyTheme(theme.value);
        }
      });
    }
  }

  function toggleTheme() {
    theme.value = theme.value === 'light' ? 'dark' : 'light';
    applyTheme(theme.value);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(STORAGE_KEY, theme.value);
    }
  }

  function setTheme(t: Theme) {
    theme.value = t;
    applyTheme(theme.value);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(STORAGE_KEY, theme.value);
    }
  }

  return { theme, toggleTheme, setTheme };
}
