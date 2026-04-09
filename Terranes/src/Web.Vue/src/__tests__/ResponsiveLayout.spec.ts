import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import { readFileSync } from 'fs';
import { resolve } from 'path';
import HomeView from '../views/HomeView.vue';
import SkeletonCard from '../components/SkeletonCard.vue';

const styleCss = readFileSync(resolve(__dirname, '../style.css'), 'utf-8');

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

describe('Responsive Layout', () => {
  it('style.css uses Bootstrap md breakpoint (768px) not 641px', () => {
    expect(styleCss).not.toContain('641px');
    expect(styleCss).toContain('768px');
    expect(styleCss).toContain('767.98px');
  });

  it('sidebar nav-scrollable uses slide animation via max-height transition', () => {
    expect(styleCss).toContain('max-height');
    expect(styleCss).toContain('transition');
    expect(styleCss).toMatch(/\.nav-scrollable\s*\{[^}]*max-height:\s*0/);
    expect(styleCss).toMatch(/\.nav-scrollable\.open\s*\{[^}]*max-height:\s*100vh/);
  });

  it('respects prefers-reduced-motion for sidebar animation', () => {
    expect(styleCss).toContain('prefers-reduced-motion');
    expect(styleCss).toMatch(/prefers-reduced-motion[\s\S]*\.nav-scrollable[\s\S]*transition:\s*none/);
  });

  it('HomeView card columns include col-12 for mobile stacking', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const cols = wrapper.findAll('.col-12.col-md-4');
    // 6 feature cards + 4 how-it-works steps use col-12
    expect(cols.length).toBeGreaterThanOrEqual(6);
  });

  it('SkeletonCard includes col-12 for mobile stacking', () => {
    const wrapper = mount(SkeletonCard, { props: { count: 3, columns: 3 } });
    const cols = wrapper.findAll('.col-12');
    expect(cols.length).toBe(3);
    expect(cols[0].classes()).toContain('col-md-4');
  });
});
