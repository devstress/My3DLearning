import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import type { Partner } from '../types';

vi.mock('../api/client', () => ({
  api: {
    getBuilders: vi.fn(),
    getBuilderProfile: vi.fn(),
  },
}));

import { api } from '../api/client';
import PartnersView from '../views/PartnersView.vue';

const mockBuilders: Partner[] = [
  {
    id: 'b1', name: 'Ace Builders', category: 'Builder',
    description: 'Quality home builder', contactEmail: 'info@ace.demo',
    isActive: true,
  },
  {
    id: 'b2', name: 'Summit Construction', category: 'Builder',
    description: 'Luxury builds', contactEmail: 'hello@summit.demo',
    isActive: false,
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

describe('PartnersView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading skeleton initially', () => {
    vi.mocked(api.getBuilders).mockReturnValue(new Promise(() => {}));
    const wrapper = mount(PartnersView, {
      global: { plugins: [createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] })] },
    });
    expect(wrapper.find('.placeholder-glow').exists()).toBe(true);
  });

  it('displays partner cards after load', async () => {
    vi.mocked(api.getBuilders).mockResolvedValue(mockBuilders);
    const router = await createTestRouter();
    const wrapper = mount(PartnersView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Ace Builders');
    expect(wrapper.text()).toContain('Summit Construction');
    // Also includes static partners
    expect(wrapper.text()).toContain('GreenScape Gardens');
  });

  it('shows empty state when no results match filter', async () => {
    vi.useFakeTimers();
    vi.mocked(api.getBuilders).mockResolvedValue([]);
    const router = await createTestRouter();
    const wrapper = mount(PartnersView, { global: { plugins: [router] } });
    await flushPromises();
    // Set search name to something that won't match any static partners
    await wrapper.find('input[type="text"]').setValue('zzzznonexistent');
    vi.advanceTimersByTime(400);
    await flushPromises();
    expect(wrapper.text()).toContain('No partners found');
    vi.useRealTimers();
  });

  it('category filter works', async () => {
    vi.mocked(api.getBuilders).mockResolvedValue(mockBuilders);
    const router = await createTestRouter();
    const wrapper = mount(PartnersView, { global: { plugins: [router] } });
    await flushPromises();
    // Select "Landscaper" category
    await wrapper.find('select').setValue('Landscaper');
    await flushPromises();
    // Should only show landscaper
    expect(wrapper.text()).toContain('GreenScape Gardens');
    expect(wrapper.text()).not.toContain('Ace Builders');
  });

  it('opens detail modal when clicking View Details', async () => {
    vi.mocked(api.getBuilders).mockResolvedValue(mockBuilders);
    const router = await createTestRouter();
    const wrapper = mount(PartnersView, { global: { plugins: [router] } });
    await flushPromises();
    const viewBtn = wrapper.findAll('button').find((b) => b.text().includes('View Details'));
    await viewBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.find('.modal').exists()).toBe(true);
  });
});
