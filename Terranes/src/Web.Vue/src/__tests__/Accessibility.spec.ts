import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import { readFileSync } from 'fs';
import { resolve } from 'path';
import DetailModal from '../components/DetailModal.vue';
import App from '../App.vue';

const styleCss = readFileSync(resolve(__dirname, '../style.css'), 'utf-8');

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div>Home</div>' } },
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

describe('Accessibility & Keyboard Navigation', () => {
  it('DetailModal has role="dialog" and aria-modal', () => {
    const wrapper = mount(DetailModal, { props: { show: true, title: 'Test' } });
    const modal = wrapper.find('[role="dialog"]');
    expect(modal.exists()).toBe(true);
    expect(modal.attributes('aria-modal')).toBe('true');
    expect(modal.attributes('aria-label')).toBe('Test');
  });

  it('DetailModal close button has aria-label', () => {
    const wrapper = mount(DetailModal, { props: { show: true, title: 'Test' } });
    const closeBtn = wrapper.find('.btn-close');
    expect(closeBtn.attributes('aria-label')).toBe('Close modal');
  });

  it('DetailModal emits close on Escape keydown', async () => {
    const wrapper = mount(DetailModal, {
      props: { show: false, title: 'Test' },
      attachTo: document.body,
    });

    // Open the modal to trigger the watcher
    await wrapper.setProps({ show: true });
    await wrapper.vm.$nextTick();

    const event = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true });
    document.dispatchEvent(event);
    await wrapper.vm.$nextTick();

    expect(wrapper.emitted('close')).toBeTruthy();
    wrapper.unmount();
  });

  it('DetailModal emits close when clicking backdrop', async () => {
    const wrapper = mount(DetailModal, { props: { show: true, title: 'Test' } });
    await wrapper.find('.modal').trigger('click');
    expect(wrapper.emitted('close')).toBeTruthy();
  });

  it('App has skip-to-content link', async () => {
    const router = await createTestRouter();
    const wrapper = mount(App, { global: { plugins: [router] } });
    const skipLink = wrapper.find('.skip-to-content');
    expect(skipLink.exists()).toBe(true);
    expect(skipLink.attributes('href')).toBe('#main-content');
    expect(skipLink.text()).toBe('Skip to content');
  });

  it('App has main element with id="main-content"', async () => {
    const router = await createTestRouter();
    const wrapper = mount(App, { global: { plugins: [router] } });
    const main = wrapper.find('main#main-content');
    expect(main.exists()).toBe(true);
  });

  it('App sidebar nav has aria-label', async () => {
    const router = await createTestRouter();
    const wrapper = mount(App, { global: { plugins: [router] } });
    const nav = wrapper.find('nav[aria-label="Main navigation"]');
    expect(nav.exists()).toBe(true);
  });

  it('App navbar toggler has aria-label', async () => {
    const router = await createTestRouter();
    const wrapper = mount(App, { global: { plugins: [router] } });
    const toggler = wrapper.find('.navbar-toggler');
    expect(toggler.attributes('aria-label')).toBe('Toggle navigation');
  });

  it('skip-to-content CSS positions link off-screen until focused', () => {
    expect(styleCss).toContain('.skip-to-content');
    expect(styleCss).toMatch(/\.skip-to-content\s*\{[^}]*top:\s*-100%/);
    expect(styleCss).toMatch(/\.skip-to-content:focus\s*\{[^}]*top:\s*0/);
  });

  it('ToastContainer has aria-live region', () => {
    // ToastContainer already has aria-live="polite" — verify it exists in source
    const toastSource = readFileSync(resolve(__dirname, '../components/ToastContainer.vue'), 'utf-8');
    expect(toastSource).toContain('aria-live="polite"');
  });
});
