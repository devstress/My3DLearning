import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import PartnersView from '../views/PartnersView.vue';

async function mountPartnersView() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/partners', component: PartnersView },
    ],
  });
  await router.push('/partners');
  await router.isReady();
  return mount(PartnersView, { global: { plugins: [router] } });
}

describe('PartnersView', () => {
  it('renders partners heading', async () => {
    const wrapper = await mountPartnersView();
    expect(wrapper.text()).toContain('Partners');
  });

  it('shows 6 partner type tabs', async () => {
    const wrapper = await mountPartnersView();
    const tabs = wrapper.findAll('[role="tab"]');
    expect(tabs.length).toBe(6);
  });

  it('builder tab is active by default', async () => {
    const wrapper = await mountPartnersView();
    const activeTab = wrapper.find('[role="tab"][aria-selected="true"]');
    expect(activeTab.text()).toContain('Builders');
  });
});
