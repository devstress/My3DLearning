import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import BreadcrumbBar from '../../components/BreadcrumbBar.vue';

function createTestRouter(routes: Array<{ path: string; name?: string; component: object; meta?: Record<string, unknown> }>) {
  return createRouter({
    history: createMemoryHistory(),
    routes,
  });
}

describe('BreadcrumbBar', () => {
  it('renders nothing on the home route (only 1 crumb)', async () => {
    const router = createTestRouter([
      { path: '/', name: 'home', component: { template: '<div />' }, meta: { breadcrumb: 'Home' } },
    ]);
    await router.push('/');
    await router.isReady();
    const wrapper = mount(BreadcrumbBar, { global: { plugins: [router] } });
    expect(wrapper.find('nav').exists()).toBe(false);
  });

  it('renders breadcrumbs on a child route', async () => {
    const router = createTestRouter([
      { path: '/', name: 'home', component: { template: '<div />' }, meta: { breadcrumb: 'Home' } },
      { path: '/villages', name: 'villages', component: { template: '<div />' }, meta: { breadcrumb: 'Villages' } },
    ]);
    await router.push('/villages');
    await router.isReady();
    const wrapper = mount(BreadcrumbBar, { global: { plugins: [router] } });
    expect(wrapper.find('nav').exists()).toBe(true);
    expect(wrapper.findAll('.breadcrumb-item').length).toBe(2);
  });

  it('shows Home as a link and the current page as plain text', async () => {
    const router = createTestRouter([
      { path: '/', name: 'home', component: { template: '<div />' }, meta: { breadcrumb: 'Home' } },
      { path: '/land', name: 'land', component: { template: '<div />' }, meta: { breadcrumb: 'Land Blocks' } },
    ]);
    await router.push('/land');
    await router.isReady();
    const wrapper = mount(BreadcrumbBar, { global: { plugins: [router] } });
    const items = wrapper.findAll('.breadcrumb-item');
    expect(items[0].find('a').exists()).toBe(true);
    expect(items[0].text()).toBe('Home');
    expect(items[1].find('a').exists()).toBe(false);
    expect(items[1].text()).toBe('Land Blocks');
  });

  it('marks the last breadcrumb as active with aria-current', async () => {
    const router = createTestRouter([
      { path: '/', name: 'home', component: { template: '<div />' }, meta: { breadcrumb: 'Home' } },
      { path: '/dashboard', name: 'dashboard', component: { template: '<div />' }, meta: { breadcrumb: 'Dashboard' } },
    ]);
    await router.push('/dashboard');
    await router.isReady();
    const wrapper = mount(BreadcrumbBar, { global: { plugins: [router] } });
    const last = wrapper.findAll('.breadcrumb-item').pop()!;
    expect(last.classes()).toContain('active');
    expect(last.attributes('aria-current')).toBe('page');
  });

  it('has aria-label on nav element', async () => {
    const router = createTestRouter([
      { path: '/', name: 'home', component: { template: '<div />' }, meta: { breadcrumb: 'Home' } },
      { path: '/villages', name: 'villages', component: { template: '<div />' }, meta: { breadcrumb: 'Villages' } },
    ]);
    await router.push('/villages');
    await router.isReady();
    const wrapper = mount(BreadcrumbBar, { global: { plugins: [router] } });
    expect(wrapper.find('nav').attributes('aria-label')).toBe('Breadcrumb');
  });
});
