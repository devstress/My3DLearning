import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import App from '../App.vue';
import DetailModal from '../components/DetailModal.vue';

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/villages', component: { template: '<div />' } },
      { path: '/home-models', component: { template: '<div />' } },
      { path: '/land', component: { template: '<div />' } },
      { path: '/marketplace', component: { template: '<div />' } },
      { path: '/journey', component: { template: '<div />' } },
      { path: '/dashboard', component: { template: '<div />' } },
    ],
  });
  await router.push('/');
  await router.isReady();
  return router;
}

describe('Accessibility', () => {
  it('skip-to-content link exists in App', async () => {
    const router = await createTestRouter();
    const wrapper = mount(App, { global: { plugins: [router] } });
    const skipLink = wrapper.find('a.skip-link');
    expect(skipLink.exists()).toBe(true);
    expect(skipLink.attributes('href')).toBe('#main-content');
    expect(skipLink.text()).toBe('Skip to content');
  });

  it('main element has id="main-content"', async () => {
    const router = await createTestRouter();
    const wrapper = mount(App, { global: { plugins: [router] } });
    const main = wrapper.find('main');
    expect(main.attributes('id')).toBe('main-content');
  });

  it('navbar toggler has aria-label', async () => {
    const router = await createTestRouter();
    const wrapper = mount(App, { global: { plugins: [router] } });
    const toggler = wrapper.find('.navbar-toggler');
    expect(toggler.attributes('aria-label')).toBe('Toggle navigation');
  });

  it('DetailModal has role="dialog" and aria-modal="true"', () => {
    const wrapper = mount(DetailModal, {
      props: { show: true, title: 'Test' },
    });
    const modal = wrapper.find('.modal');
    expect(modal.attributes('role')).toBe('dialog');
    expect(modal.attributes('aria-modal')).toBe('true');
  });

  it('DetailModal close button has aria-label', () => {
    const wrapper = mount(DetailModal, {
      props: { show: true, title: 'Test' },
    });
    const closeBtn = wrapper.find('.btn-close');
    expect(closeBtn.attributes('aria-label')).toBe('Close modal');
  });

  it('DetailModal emits close on Escape key', async () => {
    const wrapper = mount(DetailModal, {
      props: { show: false, title: 'Test' },
    });
    // Trigger the watch by changing show to true
    await wrapper.setProps({ show: true });
    // Simulate Escape keydown on document
    const event = new KeyboardEvent('keydown', { key: 'Escape' });
    document.dispatchEvent(event);
    expect(wrapper.emitted('close')).toBeTruthy();
    expect(wrapper.emitted('close')!.length).toBe(1);
  });

  it('theme toggle button has aria-label', async () => {
    const router = await createTestRouter();
    const wrapper = mount(App, { global: { plugins: [router] } });
    const btn = wrapper.find('button[aria-label="Toggle dark mode"]');
    expect(btn.exists()).toBe(true);
  });
});
