import { ref, computed } from 'vue';
import type { PlatformUser } from '../types';
import { api } from '../api/client';

const currentUser = ref<PlatformUser | null>(null);

export function useAuth() {
  const isAuthenticated = computed(() => currentUser.value !== null);

  async function login(email: string, password: string): Promise<PlatformUser> {
    const user = await api.login(email, password);
    currentUser.value = user;
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('terranes-user', JSON.stringify(user));
    }
    return user;
  }

  async function register(email: string, displayName: string, password: string): Promise<PlatformUser> {
    const user = await api.register({ email, displayName }, password);
    currentUser.value = user;
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('terranes-user', JSON.stringify(user));
    }
    return user;
  }

  function logout() {
    currentUser.value = null;
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem('terranes-user');
    }
  }

  function restoreSession() {
    if (typeof localStorage !== 'undefined') {
      const stored = localStorage.getItem('terranes-user');
      if (stored) {
        try {
          currentUser.value = JSON.parse(stored);
        } catch {
          localStorage.removeItem('terranes-user');
        }
      }
    }
  }

  return { currentUser, isAuthenticated, login, register, logout, restoreSession };
}

/** Test-only reset */
export function _resetAuth() {
  currentUser.value = null;
}
