import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import type { Walkthrough } from '../types';

vi.mock('../api/client', () => ({
  api: {
    getWalkthroughsByModel: vi.fn(),
    getWalkthroughPois: vi.fn(),
    generateWalkthrough: vi.fn(),
  },
}));

import { api } from '../api/client';
import WalkthroughsView from '../views/WalkthroughsView.vue';

const mockWalkthroughs: Walkthrough[] = [
  {
    id: 'wt1',
    homeModelId: '00000000-0000-0000-0000-000000000001',
    userId: 'u1',
    scenes: [
      { id: 's1', walkthroughId: 'wt1', sceneName: 'Entrance', sceneOrder: 1, durationSeconds: 30 },
      { id: 's2', walkthroughId: 'wt1', sceneName: 'Living Room', sceneOrder: 2, durationSeconds: 45 },
    ],
    generatedUtc: '2026-03-01T00:00:00Z',
  },
];

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }],
  });
  await router.push('/');
  await router.isReady();
  return router;
}

describe('WalkthroughsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading skeleton initially', () => {
    vi.mocked(api.getWalkthroughsByModel).mockReturnValue(new Promise(() => {}));
    const wrapper = mount(WalkthroughsView, {
      global: { plugins: [createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] })] },
    });
    expect(wrapper.find('.placeholder-glow').exists()).toBe(true);
  });

  it('shows empty state when no walkthroughs', async () => {
    vi.mocked(api.getWalkthroughsByModel).mockResolvedValue([]);
    const router = await createTestRouter();
    const wrapper = mount(WalkthroughsView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('No walkthroughs found');
  });

  it('shows walkthrough cards when data loaded', async () => {
    vi.mocked(api.getWalkthroughsByModel).mockResolvedValue(mockWalkthroughs);
    const router = await createTestRouter();
    const wrapper = mount(WalkthroughsView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Walkthrough');
    expect(wrapper.text()).toContain('2 scenes');
  });

  it('opens detail modal when clicking View Details', async () => {
    vi.mocked(api.getWalkthroughsByModel).mockResolvedValue(mockWalkthroughs);
    vi.mocked(api.getWalkthroughPois).mockResolvedValue([]);
    const router = await createTestRouter();
    const wrapper = mount(WalkthroughsView, { global: { plugins: [router] } });
    await flushPromises();
    const viewBtn = wrapper.findAll('button').find((b) => b.text().includes('View Details'));
    await viewBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.find('.modal').exists()).toBe(true);
    expect(wrapper.text()).toContain('Scenes (2)');
  });
});
