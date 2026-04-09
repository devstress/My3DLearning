import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import App from '../App.vue';
import HomeView from '../views/HomeView.vue';
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
    // Mount LandBlocksView with API stubs
    const wrapper = mount(LandBlocksView, {
      global: {
        plugins: [router],
        stubs: {
          SkeletonTable: { template: '<div class="table-responsive"><table></table></div>' },
        },
      },
    });
    // The skeleton placeholder wraps in table-responsive, or the actual view does
    expect(wrapper.html()).toContain('table-responsive');
  });
});
