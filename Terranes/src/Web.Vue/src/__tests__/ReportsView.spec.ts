import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import ReportsView from '../views/ReportsView.vue';

async function mountReportsView() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/reports', component: ReportsView },
    ],
  });
  await router.push('/reports');
  await router.isReady();
  return mount(ReportsView, { global: { plugins: [router] } });
}

describe('ReportsView', () => {
  it('renders reports heading', async () => {
    const wrapper = await mountReportsView();
    expect(wrapper.text()).toContain('Reports');
  });

  it('has report type select', async () => {
    const wrapper = await mountReportsView();
    expect(wrapper.find('select[aria-label="Report type"]').exists()).toBe(true);
  });

  it('has generate button', async () => {
    const wrapper = await mountReportsView();
    expect(wrapper.text()).toContain('Generate');
  });
});
