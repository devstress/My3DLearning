import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import LoginView from '../views/LoginView.vue';

async function mountLoginView() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/login', component: LoginView },
      { path: '/register', component: { template: '<div />' } },
      { path: '/dashboard', component: { template: '<div />' } },
    ],
  });
  await router.push('/login');
  await router.isReady();
  return mount(LoginView, { global: { plugins: [router] } });
}

describe('LoginView', () => {
  it('renders login heading', async () => {
    const wrapper = await mountLoginView();
    expect(wrapper.text()).toContain('Login');
  });

  it('has email and password inputs', async () => {
    const wrapper = await mountLoginView();
    expect(wrapper.find('#login-email').exists()).toBe(true);
    expect(wrapper.find('#login-password').exists()).toBe(true);
  });

  it('has a sign in button', async () => {
    const wrapper = await mountLoginView();
    expect(wrapper.text()).toContain('Sign In');
  });

  it('has a link to register', async () => {
    const wrapper = await mountLoginView();
    expect(wrapper.text()).toContain('Register here');
    expect(wrapper.find('a[href="/register"]').exists()).toBe(true);
  });
});
