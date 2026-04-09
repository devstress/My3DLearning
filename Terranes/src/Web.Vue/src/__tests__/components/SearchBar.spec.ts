import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import SearchBar from '../../components/SearchBar.vue';

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/search', component: { template: '<div />' } },
    ],
  });
  await router.push('/');
  await router.isReady();
  return router;
}

describe('SearchBar', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders input with placeholder', async () => {
    const router = await createTestRouter();
    const wrapper = mount(SearchBar, { global: { plugins: [router] } });
    const input = wrapper.find('input');
    expect(input.exists()).toBe(true);
    expect(input.attributes('placeholder')).toBe('Search...');
  });

  it('navigates on Enter key', async () => {
    const router = await createTestRouter();
    const pushSpy = vi.spyOn(router, 'push');
    const wrapper = mount(SearchBar, { global: { plugins: [router] } });
    const input = wrapper.find('input');
    await input.setValue('test query');
    await input.trigger('keydown.enter');
    await flushPromises();
    expect(pushSpy).toHaveBeenCalledWith({ path: '/search', query: { query: 'test query' } });
  });

  it('has magnifying glass icon', async () => {
    const router = await createTestRouter();
    const wrapper = mount(SearchBar, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain('🔍');
  });
});
