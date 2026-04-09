import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import SearchView from '../views/SearchView.vue';

async function mountSearchView() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/search', component: SearchView },
      { path: '/home-models', component: { template: '<div />' } },
    ],
  });
  await router.push('/search');
  await router.isReady();
  return mount(SearchView, { global: { plugins: [router] } });
}

describe('SearchView', () => {
  it('renders search heading', async () => {
    const wrapper = await mountSearchView();
    expect(wrapper.text()).toContain('Search');
  });

  it('has a search input', async () => {
    const wrapper = await mountSearchView();
    expect(wrapper.find('input[type="search"], input[placeholder*="Search"]').exists() || wrapper.findComponent({ name: 'SearchBar' }).exists()).toBe(true);
  });

  it('has an entity type filter dropdown', async () => {
    const wrapper = await mountSearchView();
    expect(wrapper.find('select[aria-label="Filter by entity type"]').exists()).toBe(true);
  });

  it('displays entity type options', async () => {
    const wrapper = await mountSearchView();
    const options = wrapper.findAll('option');
    expect(options.length).toBeGreaterThanOrEqual(4);
    expect(options.some(o => o.text().includes('All Types'))).toBe(true);
  });
});
