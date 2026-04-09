import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import BreadcrumbBar from '../../components/BreadcrumbBar.vue';

async function mountWithRoute(path: string, routeMeta: Record<string, unknown> = {}) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'home', component: { template: '<div />' }, meta: { breadcrumb: 'Home' } },
      { path: '/dashboard', name: 'dashboard', component: { template: '<div />' }, meta: { breadcrumb: 'Dashboard', ...routeMeta } },
      { path: '/villages', name: 'villages', component: { template: '<div />' }, meta: { breadcrumb: 'Villages' } },
    ],
  });
  await router.push(path);
  await router.isReady();
  const wrapper = mount(BreadcrumbBar, {
    global: { plugins: [router] },
  });
  return wrapper;
}

describe('BreadcrumbBar', () => {
  it('renders Home as first breadcrumb on non-home pages', async () => {
    const wrapper = await mountWithRoute('/dashboard');
    const items = wrapper.findAll('.breadcrumb-item');
    expect(items.length).toBeGreaterThanOrEqual(2);
    expect(items[0].text()).toBe('Home');
  });

  it('shows current page as active breadcrumb', async () => {
    const wrapper = await mountWithRoute('/dashboard');
    const items = wrapper.findAll('.breadcrumb-item');
    const last = items[items.length - 1];
    expect(last.classes()).toContain('active');
    expect(last.text()).toBe('Dashboard');
  });

  it('generates breadcrumb from route meta', async () => {
    const wrapper = await mountWithRoute('/villages');
    expect(wrapper.text()).toContain('Villages');
    expect(wrapper.text()).toContain('Home');
  });
});
