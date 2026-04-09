import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import WalkthroughsView from '../views/WalkthroughsView.vue';

async function mountWalkthroughsView() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/walkthroughs', component: WalkthroughsView },
    ],
  });
  await router.push('/walkthroughs');
  await router.isReady();
  return mount(WalkthroughsView, { global: { plugins: [router] } });
}

describe('WalkthroughsView', () => {
  it('renders walkthroughs heading', async () => {
    const wrapper = await mountWalkthroughsView();
    expect(wrapper.text()).toContain('Walkthroughs');
  });

  it('has a select model prompt', async () => {
    const wrapper = await mountWalkthroughsView();
    expect(wrapper.text()).toContain('Select a Home Design');
  });

  it('shows prompt to select model when none selected', async () => {
    const wrapper = await mountWalkthroughsView();
    expect(wrapper.text()).toContain('Select a home design to view');
  });
});
