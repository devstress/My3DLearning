import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import RegisterView from '../views/RegisterView.vue';

async function mountRegisterView() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/register', component: RegisterView },
      { path: '/login', component: { template: '<div />' } },
      { path: '/dashboard', component: { template: '<div />' } },
    ],
  });
  await router.push('/register');
  await router.isReady();
  return mount(RegisterView, { global: { plugins: [router] } });
}

describe('RegisterView', () => {
  it('renders register heading', async () => {
    const wrapper = await mountRegisterView();
    expect(wrapper.text()).toContain('Register');
  });

  it('has all four registration fields', async () => {
    const wrapper = await mountRegisterView();
    expect(wrapper.find('#reg-name').exists()).toBe(true);
    expect(wrapper.find('#reg-email').exists()).toBe(true);
    expect(wrapper.find('#reg-password').exists()).toBe(true);
    expect(wrapper.find('#reg-confirm').exists()).toBe(true);
  });

  it('has a create account button', async () => {
    const wrapper = await mountRegisterView();
    expect(wrapper.text()).toContain('Create Account');
  });

  it('has a link to login', async () => {
    const wrapper = await mountRegisterView();
    expect(wrapper.text()).toContain('Sign in');
    expect(wrapper.find('a[href="/login"]').exists()).toBe(true);
  });
});
