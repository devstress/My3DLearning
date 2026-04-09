import { ref, computed, readonly } from 'vue';
import type { PlatformUser } from '../types';

const currentUser = ref<PlatformUser | null>(null);
const isLoading = ref(false);
const error = ref<string | null>(null);

const isAuthenticated = computed(() => currentUser.value !== null);
const displayName = computed(() => currentUser.value?.displayName ?? 'Guest');

function setUser(user: PlatformUser | null) {
  currentUser.value = user;
  if (user) {
    localStorage.setItem('terranes_user', JSON.stringify(user));
  } else {
    localStorage.removeItem('terranes_user');
  }
}

function loadStoredUser() {
  const stored = localStorage.getItem('terranes_user');
  if (stored) {
    try {
      currentUser.value = JSON.parse(stored);
    } catch {
      localStorage.removeItem('terranes_user');
    }
  }
}

// Load on module init
loadStoredUser();

export function useAuth() {
  function logout() {
    setUser(null);
    error.value = null;
  }

  function clearError() {
    error.value = null;
  }

  return {
    user: readonly(currentUser),
    isAuthenticated,
    isLoading: readonly(isLoading),
    error: readonly(error),
    displayName,
    setUser,
    logout,
    clearError,
  };
}
