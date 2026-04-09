import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import type { SearchResult } from '../types';

vi.mock('../api/client', () => ({
  api: {
    search: vi.fn(),
    searchByType: vi.fn(),
  },
}));

import { api } from '../api/client';
import SearchView from '../views/SearchView.vue';

const mockResults: SearchResult[] = [
  { entityType: 'HomeModel', entityId: 'h1', title: 'Modern Villa', summary: 'A stylish home', relevanceScore: 0.95 },
  { entityType: 'Village', entityId: 'v1', title: 'Sunset Cove', summary: 'Coastal village', relevanceScore: 0.85 },
];

async function createTestRouter(query: Record<string, string> = {}) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/search', component: { template: '<div />' } },
    ],
  });
  await router.push({ path: '/search', query });
  await router.isReady();
  return router;
}

describe('SearchView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading skeleton while fetching', async () => {
    vi.mocked(api.search).mockReturnValue(new Promise(() => {}));
    const router = await createTestRouter({ query: 'villa' });
    const wrapper = mount(SearchView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.find('.placeholder-glow').exists()).toBe(true);
  });

  it('displays results after fetch', async () => {
    vi.mocked(api.search).mockResolvedValue(mockResults);
    const router = await createTestRouter({ query: 'villa' });
    const wrapper = mount(SearchView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Modern Villa');
    expect(wrapper.text()).toContain('Sunset Cove');
  });

  it('shows empty state when no results', async () => {
    vi.mocked(api.search).mockResolvedValue([]);
    const router = await createTestRouter({ query: 'nonexistent' });
    const wrapper = mount(SearchView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('No results found');
  });

  it('filters by entity type', async () => {
    vi.mocked(api.searchByType).mockResolvedValue([mockResults[0]]);
    const router = await createTestRouter({ query: 'villa', type: 'HomeModel' });
    const wrapper = mount(SearchView, { global: { plugins: [router] } });
    await flushPromises();
    expect(api.searchByType).toHaveBeenCalledWith('HomeModel', 'villa');
    expect(wrapper.text()).toContain('Modern Villa');
  });

  it('shows result count badge', async () => {
    vi.mocked(api.search).mockResolvedValue(mockResults);
    const router = await createTestRouter({ query: 'villa' });
    const wrapper = mount(SearchView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.find('.result-count').text()).toContain('2');
  });
});
