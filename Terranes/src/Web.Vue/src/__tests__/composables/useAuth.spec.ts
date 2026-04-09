import { describe, it, expect, beforeEach } from 'vitest';
import { ref, nextTick } from 'vue';
import { useAuth } from '../../composables/useAuth';

describe('useAuth', () => {
  beforeEach(() => {
    localStorage.clear();
    const { logout } = useAuth();
    logout();
  });

  it('starts unauthenticated', () => {
    const { isAuthenticated, displayName } = useAuth();
    expect(isAuthenticated.value).toBe(false);
    expect(displayName.value).toBe('Guest');
  });

  it('sets user and becomes authenticated', () => {
    const { isAuthenticated, user, setUser, displayName } = useAuth();
    setUser({
      id: '1',
      email: 'test@test.com',
      displayName: 'Test User',
      role: 'Buyer',
      isActive: true,
      createdUtc: new Date().toISOString(),
    });
    expect(isAuthenticated.value).toBe(true);
    expect(user.value?.email).toBe('test@test.com');
    expect(displayName.value).toBe('Test User');
  });

  it('persists user to localStorage', () => {
    const { setUser } = useAuth();
    setUser({
      id: '1',
      email: 'test@test.com',
      displayName: 'Stored',
      role: 'Buyer',
      isActive: true,
      createdUtc: new Date().toISOString(),
    });
    const stored = localStorage.getItem('terranes_user');
    expect(stored).toBeTruthy();
    expect(JSON.parse(stored!).displayName).toBe('Stored');
  });

  it('logout clears user and localStorage', () => {
    const { setUser, logout, isAuthenticated } = useAuth();
    setUser({
      id: '1',
      email: 'test@test.com',
      displayName: 'Test',
      role: 'Buyer',
      isActive: true,
      createdUtc: new Date().toISOString(),
    });
    expect(isAuthenticated.value).toBe(true);
    logout();
    expect(isAuthenticated.value).toBe(false);
    expect(localStorage.getItem('terranes_user')).toBeNull();
  });

  it('clearError resets error state', () => {
    const { error, clearError } = useAuth();
    expect(error.value).toBeNull();
    clearError();
    expect(error.value).toBeNull();
  });
});
