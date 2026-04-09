import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import { _resetAuth } from '../composables/useAuth';

vi.mock('../api/client', () => ({
  api: {
    login: vi.fn(),
  },
}));

import { api } from '../api/client';
import LoginView from '../views/LoginView.vue';

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/login', component: { template: '<div />' } },
      { path: '/dashboard', component: { template: '<div />' } },
      { path: '/register', component: { template: '<div />' } },
    ],
  });
  await router.push('/login');
  await router.isReady();
  return router;
}

describe('LoginView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    _resetAuth();
  });

  it('shows email and password inputs', async () => {
    const router = await createTestRouter();
    const wrapper = mount(LoginView, { global: { plugins: [router] } });
    expect(wrapper.find('input#email').exists()).toBe(true);
    expect(wrapper.find('input#password').exists()).toBe(true);
  });

  it('shows error on failed login', async () => {
    vi.mocked(api.login).mockRejectedValue(new Error('Invalid credentials'));
    const router = await createTestRouter();
    const wrapper = mount(LoginView, { global: { plugins: [router] } });
    await wrapper.find('input#email').setValue('bad@test.com');
    await wrapper.find('input#password').setValue('wrong');
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    expect(wrapper.text()).toContain('Invalid credentials');
  });

  it('disables button during loading', async () => {
    vi.mocked(api.login).mockReturnValue(new Promise(() => {}));
    const router = await createTestRouter();
    const wrapper = mount(LoginView, { global: { plugins: [router] } });
    await wrapper.find('input#email').setValue('test@test.com');
    await wrapper.find('input#password').setValue('pass');
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    const btn = wrapper.find('button[type="button"]');
    expect(btn.attributes('disabled')).toBeDefined();
  });
});
