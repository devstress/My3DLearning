import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import type { PropertyListing } from '../types';

vi.mock('../api/client', () => ({
  api: {
    getListings: vi.fn(),
  },
}));

import { api } from '../api/client';
import MarketplaceView from '../views/MarketplaceView.vue';

const mockListings: PropertyListing[] = [
  {
    id: 'p1', title: 'Modern Beach House', description: 'Beautiful beachfront property',
    homeModelId: 'hm1', landBlockId: 'lb1', askingPriceAud: 750000,
    status: 'Active', listedUtc: '2026-03-01T00:00:00Z',
  },
  {
    id: 'p2', title: 'Budget Studio', description: 'Affordable living',
    homeModelId: 'hm2', askingPriceAud: undefined,
    status: 'Draft', listedUtc: '2026-03-15T00:00:00Z',
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

describe('MarketplaceView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state initially', () => {
    vi.mocked(api.getListings).mockReturnValue(new Promise(() => {}));
    const wrapper = mount(MarketplaceView, {
      global: { plugins: [createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] })] },
    });
    expect(wrapper.text()).toContain('Loading listings...');
  });

  it('displays listing cards after data loads', async () => {
    vi.mocked(api.getListings).mockResolvedValue(mockListings);
    const router = await createTestRouter();
    const wrapper = mount(MarketplaceView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Modern Beach House');
    expect(wrapper.text()).toContain('Budget Studio');
  });

  it('shows status badge with correct class', async () => {
    vi.mocked(api.getListings).mockResolvedValue(mockListings);
    const router = await createTestRouter();
    const wrapper = mount(MarketplaceView, { global: { plugins: [router] } });
    await flushPromises();
    const badges = wrapper.findAll('.badge');
    const activeBadge = badges.find((b) => b.text() === 'Active');
    expect(activeBadge).toBeTruthy();
    expect(activeBadge!.classes()).toContain('bg-success');
  });

  it('opens detail modal on "View Details" click', async () => {
    vi.mocked(api.getListings).mockResolvedValue(mockListings);
    const router = await createTestRouter();
    const wrapper = mount(MarketplaceView, { global: { plugins: [router] } });
    await flushPromises();
    const viewBtn = wrapper.findAll('button').find((b) => b.text().includes('View Details'));
    await viewBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.find('.modal').exists()).toBe(true);
    expect(wrapper.find('.modal-title').text()).toBe('Modern Beach House');
  });

  it('formats price correctly or shows "Price on Application"', async () => {
    vi.mocked(api.getListings).mockResolvedValue(mockListings);
    const router = await createTestRouter();
    const wrapper = mount(MarketplaceView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('$');
    expect(wrapper.text()).toContain('Price on Application');
  });
});
