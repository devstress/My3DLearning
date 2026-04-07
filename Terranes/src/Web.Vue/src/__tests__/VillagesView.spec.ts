import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import type { VirtualVillage, VillageLot } from '../types';

vi.mock('../api/client', () => ({
  api: {
    getVillages: vi.fn(),
    getVillageLots: vi.fn(),
  },
}));

import { api } from '../api/client';
import VillagesView from '../views/VillagesView.vue';

const mockVillages: VirtualVillage[] = [
  {
    id: 'v1', name: 'Sunset Cove', description: 'A coastal village',
    layoutType: 'Grid', maxLots: 20, centreLatitude: -33.8688, centreLongitude: 151.2093,
    createdUtc: '2026-01-01T00:00:00Z',
  },
  {
    id: 'v2', name: 'Mountain View', description: 'A mountain retreat',
    layoutType: 'Radial', maxLots: 12, centreLatitude: -37.8136, centreLongitude: 144.9631,
    createdUtc: '2026-02-01T00:00:00Z',
  },
];

const mockLots: VillageLot[] = [
  { id: 'l1', villageId: 'v1', lotNumber: 1, status: 'Occupied', positionX: 10.0, positionY: 20.0 },
  { id: 'l2', villageId: 'v1', lotNumber: 2, status: 'Vacant', positionX: 30.0, positionY: 40.0 },
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

describe('VillagesView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state initially', () => {
    vi.mocked(api.getVillages).mockReturnValue(new Promise(() => {}));
    const wrapper = mount(VillagesView, {
      global: { plugins: [createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] })] },
    });
    expect(wrapper.text()).toContain('Loading villages...');
  });

  it('displays village cards after data loads', async () => {
    vi.mocked(api.getVillages).mockResolvedValue(mockVillages);
    const router = await createTestRouter();
    const wrapper = mount(VillagesView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Sunset Cove');
    expect(wrapper.text()).toContain('Mountain View');
  });

  it('shows village layout type and max lots', async () => {
    vi.mocked(api.getVillages).mockResolvedValue(mockVillages);
    const router = await createTestRouter();
    const wrapper = mount(VillagesView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Grid');
    expect(wrapper.text()).toContain('Max 20 lots');
  });

  it('opens detail modal with village info and lots table', async () => {
    vi.mocked(api.getVillages).mockResolvedValue(mockVillages);
    vi.mocked(api.getVillageLots).mockResolvedValue(mockLots);
    const router = await createTestRouter();
    const wrapper = mount(VillagesView, { global: { plugins: [router] } });
    await flushPromises();
    const viewBtn = wrapper.findAll('button').find((b) => b.text().includes('View Details'));
    await viewBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.find('.modal').exists()).toBe(true);
    expect(wrapper.find('.modal-title').text()).toBe('Sunset Cove');
    expect(wrapper.text()).toContain('Lots (2)');
  });

  it('shows empty state when no villages match', async () => {
    vi.mocked(api.getVillages).mockResolvedValue([]);
    const router = await createTestRouter();
    const wrapper = mount(VillagesView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('No villages found');
  });
});
