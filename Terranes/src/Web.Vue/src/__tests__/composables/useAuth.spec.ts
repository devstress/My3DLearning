import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { PlatformUser } from '../../types';
import { useAuth, _resetAuth } from '../../composables/useAuth';

vi.mock('../../api/client', () => ({
  api: {
    login: vi.fn(),
    register: vi.fn(),
  },
}));

import { api } from '../../api/client';

const mockUser: PlatformUser = {
  id: 'u1',
  email: 'test@example.com',
  displayName: 'Test User',
  role: 'buyer',
  isActive: true,
  createdUtc: '2026-01-01T00:00:00Z',
};

describe('useAuth', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    _resetAuth();
    localStorage.clear();
  });

  it('isAuthenticated is false by default', () => {
    const { isAuthenticated } = useAuth();
    expect(isAuthenticated.value).toBe(false);
  });

  it('login sets currentUser', async () => {
    vi.mocked(api.login).mockResolvedValue(mockUser);
    const { login, currentUser, isAuthenticated } = useAuth();
    await login('test@example.com', 'password');
    expect(currentUser.value).toEqual(mockUser);
    expect(isAuthenticated.value).toBe(true);
  });

  it('logout clears currentUser', async () => {
    vi.mocked(api.login).mockResolvedValue(mockUser);
    const { login, logout, currentUser, isAuthenticated } = useAuth();
    await login('test@example.com', 'password');
    logout();
    expect(currentUser.value).toBeNull();
    expect(isAuthenticated.value).toBe(false);
  });

  it('register sets currentUser', async () => {
    vi.mocked(api.register).mockResolvedValue(mockUser);
    const { register, currentUser, isAuthenticated } = useAuth();
    await register('test@example.com', 'Test User', 'password');
    expect(currentUser.value).toEqual(mockUser);
    expect(isAuthenticated.value).toBe(true);
  });

  it('restoreSession reads from localStorage', () => {
    localStorage.setItem('terranes-user', JSON.stringify(mockUser));
    const { restoreSession, currentUser, isAuthenticated } = useAuth();
    restoreSession();
    expect(currentUser.value).toEqual(mockUser);
    expect(isAuthenticated.value).toBe(true);
  });

  it('restoreSession handles invalid JSON', () => {
    localStorage.setItem('terranes-user', 'not-json');
    const { restoreSession, currentUser } = useAuth();
    restoreSession();
    expect(currentUser.value).toBeNull();
    expect(localStorage.getItem('terranes-user')).toBeNull();
  });
});
