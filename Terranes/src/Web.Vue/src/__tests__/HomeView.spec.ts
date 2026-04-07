import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import HomeView from '../views/HomeView.vue';

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

describe('HomeView', () => {
  it('renders page title "Welcome to Terranes"', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain('Welcome to Terranes');
  });

  it('shows all 6 feature cards', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const cards = wrapper.findAll('.card');
    expect(cards.length).toBe(6);
  });

  it('has correct router-link to /villages', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const links = wrapper.findAll('a');
    const hrefs = links.map((l) => l.attributes('href'));
    expect(hrefs).toContain('/villages');
  });

  it('has correct router-link to /home-models', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const links = wrapper.findAll('a');
    const hrefs = links.map((l) => l.attributes('href'));
    expect(hrefs).toContain('/home-models');
  });

  it('has correct router-link paths for all 6 destinations', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const links = wrapper.findAll('a');
    const hrefs = links.map((l) => l.attributes('href'));
    expect(hrefs).toContain('/villages');
    expect(hrefs).toContain('/home-models');
    expect(hrefs).toContain('/land');
    expect(hrefs).toContain('/marketplace');
    expect(hrefs).toContain('/journey');
    expect(hrefs).toContain('/dashboard');
  });

  it('renders card titles', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const text = wrapper.text();
    expect(text).toContain('Virtual Villages');
    expect(text).toContain('Home Designs');
    expect(text).toContain('Find Land');
    expect(text).toContain('Marketplace');
    expect(text).toContain('Start Your Journey');
    expect(text).toContain('Dashboard');
  });
});
