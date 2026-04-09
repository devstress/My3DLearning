import { describe, it, expect, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import App from '../App.vue';
import HomeView from '../views/HomeView.vue';

vi.mock('../api/client', () => ({
  api: {
    getLandBlocks: vi.fn().mockResolvedValue([
      { id: 'b1', address: '10 Main St', suburb: 'Surry Hills', state: 'NSW', areaSqm: 450, frontageMetre: 15, depthMetre: 30, zoning: 'R2' },
    ]),
    getHomeModels: vi.fn().mockResolvedValue([]),
    createSitePlacement: vi.fn().mockResolvedValue({}),
  },
}));

import LandBlocksView from '../views/LandBlocksView.vue';

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: HomeView },
      { path: '/villages', component: { template: '<div />' } },
      { path: '/home-models', component: { template: '<div />' } },
      { path: '/land', component: LandBlocksView },
      { path: '/marketplace', component: { template: '<div />' } },
      { path: '/journey', component: { template: '<div />' } },
      { path: '/dashboard', component: { template: '<div />' } },
    ],
  });
  await router.push('/');
  await router.isReady();
  return router;
}

describe('Responsive Layout', () => {
  it('sidebar nav-scrollable is rendered', async () => {
    const router = await createTestRouter();
    const wrapper = mount(App, { global: { plugins: [router] } });
    expect(wrapper.find('.nav-scrollable').exists()).toBe(true);
  });

  it('sidebar toggle button exists', async () => {
    const router = await createTestRouter();
    const wrapper = mount(App, { global: { plugins: [router] } });
    expect(wrapper.find('.navbar-toggler').exists()).toBe(true);
  });

  it('HomeView has mobile-first column classes (col-12 col-md-4)', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const cols = wrapper.findAll('.col-12.col-md-4');
    expect(cols.length).toBeGreaterThanOrEqual(1);
  });

  it('LandBlocksView has table-responsive class', async () => {
    const router = await createTestRouter();
    const wrapper = mount(LandBlocksView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.html()).toContain('table-responsive');
  });
});
